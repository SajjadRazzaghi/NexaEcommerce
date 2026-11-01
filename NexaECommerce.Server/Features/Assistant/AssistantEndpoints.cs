using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Features.Inventory;
using NexaECommerce.Server.Features.Orders;
using NexaECommerce.Server.Features.PointOfSale;
using NexaECommerce.Server.Features.Products;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Assistant;

/// <summary>
/// First release of the Nexa assistant. It uses a small local intent router and read-only,
/// tenant-scoped database queries. It intentionally has no external LLM/provider integration.
/// Any future external provider must be separately approved and disclosed before it is enabled.
/// </summary>
public sealed class AssistantEndpoints : IFeatureEndpoints
{
    private const int MaxQuestionLength = 1000;
    private const int MaxDetails = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant")
            .WithTags("Assistant")
            .RequireAuthorization()
            .RequirePermission(AssistantPermissions.Use);

        group.MapGet("/info", GetInfo);
        group.MapPost("/query", Query);
    }

    private static IResult GetInfo([FromServices] IConfiguration configuration)
    {
        var processingLocation = configuration["Assistant:ProcessingLocation"];

        return Results.Ok(new
        {
            mode = "local-only",
            providerName = "Nexa local query router",
            externalProviderConnected = false,
            dataSentExternally = false,
            dataCategories = new[]
            {
                "متن پرسشی که کاربر به سرور نکسا وارد می‌کند",
                "خلاصه و رکوردهای مجازِ شرکت فعال، فقط در محدودهٔ مجوز کاربر"
            },
            processingLocation = string.IsNullOrWhiteSpace(processingLocation) ? null : processingLocation,
            externalApiCost = "0 — no external AI API is called",
            developmentCostSeparate = true,
            developmentCostNote = "Development cost is separate from API consumption and must be quoted independently.",
            writtenApprovalRequiredBeforeExternalUse = true,
            writeActionsEnabled = false,
            businessTimeZoneId = GetBusinessDay(configuration).TimeZoneId
        });
    }

    private static async Task<IResult> Query(
        [FromBody] AssistantQueryRequest request,
        [FromServices] ICurrentTenant tenant,
        [FromServices] OrdersDbContext ordersDb,
        [FromServices] InventoryDbContext inventoryDb,
        [FromServices] CatalogDbContext catalogDb,
        [FromServices] IInventoryReportService inventoryReports,
        [FromServices] IConfiguration configuration,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var rawQuestion = request?.Question?.Trim();
        if (string.IsNullOrWhiteSpace(rawQuestion))
        {
            return Results.BadRequest(new { error = "Question is required." });
        }

        if (rawQuestion.Length > MaxQuestionLength)
        {
            return Results.BadRequest(new { error = $"Question cannot exceed {MaxQuestionLength} characters." });
        }

        var question = Normalize(rawQuestion);
        var user = http.User;

        if (IsActionRequest(question))
        {
            return Reply(
                "این نسخهٔ دستیار فقط‌خواندنی است و هیچ endpoint اجرایی برای تغییر سفارش، کالا یا موجودی ندارد؛ هیچ تغییری انجام نشده است. پیش از فعال‌شدن هر اقدام مجاز، باید جزئیات دقیق اقدام و اثر آن نمایش داده شود و تأیید صریح کاربر در سمت سرور اعتبارسنجی شود.",
                "action-disabled",
                "read-only-policy",
                []);
        }

        if (ContainsAny(question, "کم موجودی", "موجودی کم", "رو به اتمام", "نقطه سفارش", "low stock", "low-stock"))
        {
            return await GetLowStockAsync(
                tenant, user, inventoryReports, inventoryDb, catalogDb, cancellationToken);
        }

        if (ContainsAny(question, "معوق", "سررسید گذشته", "overdue"))
        {
            return Reply(
                "در مدل فعلی سفارش‌ها، سررسید یا موعد پرداخت مستقل تعریف نشده است؛ بنابراین نمی‌توانم سفارش «معوق» را با اطمینان محاسبه کنم. برای گزارش دقیق‌تر، «سفارش‌های پرداخت‌نشده» را بپرسید.",
                "insufficient-data",
                "system-model",
                []);
        }

        if (IsUnpaidOrderQuestion(question))
        {
            return await GetUnpaidOrdersAsync(
                question, tenant, user, ordersDb, configuration, cancellationToken);
        }

        if (IsSalesSummaryQuestion(question))
        {
            return await GetSalesSummaryAsync(
                tenant, user, ordersDb, configuration, cancellationToken);
        }

        if (IsProductQuestion(question))
        {
            return await GetProductsAsync(
                question, tenant, user, inventoryDb, catalogDb, cancellationToken);
        }

        if (ContainsAny(question, "راهنما", "کمک", "چه سوال", "چطور", "چگونه", "help", "guide")
            || question.Length < 3)
        {
            return Reply(
                "می‌توانید دربارهٔ «سفارش‌های پرداخت‌نشدهٔ امروز»، «خلاصهٔ پرداخت‌های موفق امروز»، «کالاهای کم‌موجودی» یا کالایی که در موجودی شرکت فعال ثبت شده سؤال کنید. گزارش‌ها از دادهٔ زندهٔ نکسا و فقط در محدودهٔ شرکت فعال و مجوزهای شما تهیه می‌شوند. این نسخه فقط‌خواندنی است و هیچ عملیاتی را اجرا نمی‌کند.",
                "system-guide",
                "nexa-local-guide",
                []);
        }

        return Reply(
            "نتوانستم این پرسش را با قواعد محلی به یک گزارش دقیق و مجاز تبدیل کنم؛ برای جلوگیری از حدس‌زدن، پاسخی از داده‌های ناقص نمی‌سازم. پرسش را مشخص‌تر کنید؛ مثلاً «سفارش‌های پرداخت‌نشدهٔ امروز» یا «کالاهای کم‌موجودی».",
            "insufficient-data",
            "nexa-local-query-router",
            []);
    }

    private static async Task<IResult> GetUnpaidOrdersAsync(
        string question,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        OrdersDbContext ordersDb,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var canManageTenantOrders = HasPermission(user, OrderPermissions.Manage);
        if (!canManageTenantOrders && !HasPermission(user, OrderPermissions.Read))
        {
            return PermissionReply("orders.read یا orders.manage");
        }

        var query = ordersDb.Orders.AsNoTracking()
            .Where(order => order.TenantId == tenant.Id && order.Status == OrderStatus.PendingPayment);

        if (!canManageTenantOrders)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            query = query.Where(order => order.UserId == userId);
        }

        var isToday = ContainsAny(question, "امروز", "today");
        string scopeDescription = canManageTenantOrders ? "شرکت فعال" : "حساب کاربری شما";
        if (isToday)
        {
            var day = GetBusinessDay(configuration);
            query = query.Where(order => order.CreatedAt >= day.StartUtc && order.CreatedAt < day.EndUtc);
        }

        var count = await query.CountAsync(cancellationToken);
        var total = await query.SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m;
        var recent = await query.OrderByDescending(order => order.CreatedAt)
            .Take(MaxDetails)
            .Select(order => new { order.OrderNumber, order.Status, order.TotalAmount, order.CreatedAt })
            .ToListAsync(cancellationToken);

        var details = new List<AssistantDetail>
        {
            new("تعداد سفارش پرداخت‌نشده", count.ToString("N0")),
            new("جمع مبلغ سفارش‌های پرداخت‌نشده", FormatMoney(total))
        };

        details.AddRange(recent.Select(order => new AssistantDetail(
            order.OrderNumber,
            $"{StatusLabel(order.Status)} · {FormatMoney(order.TotalAmount)} · {order.CreatedAt:yyyy-MM-dd HH:mm} UTC")));

        var timeScope = isToday ? "ثبت‌شده در امروز" : "در همهٔ تاریخ‌ها";
        var answer = count == 0
            ? $"در {scopeDescription}، سفارش پرداخت‌نشده‌ای {timeScope} پیدا نشد."
            : $"در {scopeDescription}، {count:N0} سفارش پرداخت‌نشده {timeScope} وجود دارد؛ جمع مبلغ آن‌ها {FormatMoney(total)} است. حداکثر {MaxDetails} سفارش اخیر در جزئیات آمده است.";

        return Reply(answer, "live-data", "orders-db", details, DateTimeOffset.UtcNow);
    }

    private static async Task<IResult> GetSalesSummaryAsync(
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        OrdersDbContext ordersDb,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var canReadOnlineSales = HasPermission(user, OrderPermissions.Manage);
        var canReadPosSales = HasPermission(user, PosPermissions.Read);
        if (!canReadOnlineSales && !canReadPosSales)
        {
            return PermissionReply("orders.manage یا pos.read");
        }

        var day = GetBusinessDay(configuration);
        var start = new DateTimeOffset(day.StartUtc);
        var end = new DateTimeOffset(day.EndUtc);
        var details = new List<AssistantDetail>();
        var totalSales = 0m;
        var totalTransactions = 0;

        if (canReadOnlineSales)
        {
            var onlinePayments = ordersDb.PaymentAttempts.AsNoTracking()
                .Where(payment => payment.TenantId == tenant.Id
                    && payment.Status == PaymentAttemptStatus.Succeeded
                    && payment.CompletedAt.HasValue
                    && payment.CompletedAt.Value >= start
                    && payment.CompletedAt.Value < end);

            // Collapse retries to one successful payment per order to avoid double counting an order.
            var paymentsByOrder = await onlinePayments
                .GroupBy(payment => payment.OrderId)
                .Select(group => new { OrderId = group.Key, Amount = group.Max(payment => payment.Amount) })
                .ToListAsync(cancellationToken);

            var onlineTotal = paymentsByOrder.Sum(payment => payment.Amount);
            details.Add(new("پرداخت‌های موفق آنلاین", paymentsByOrder.Count.ToString("N0")));
            details.Add(new("جمع پرداخت‌های موفق آنلاین", FormatMoney(onlineTotal)));
            totalSales += onlineTotal;
            totalTransactions += paymentsByOrder.Count;
        }

        if (canReadPosSales)
        {
            var posSales = ordersDb.PosSales.AsNoTracking()
                .Where(sale => sale.TenantId == tenant.Id
                    && sale.Status == PosSaleStatus.Paid
                    && sale.PaidAtUtc.HasValue
                    && sale.PaidAtUtc.Value >= day.StartUtc
                    && sale.PaidAtUtc.Value < day.EndUtc);

            var posCount = await posSales.CountAsync(cancellationToken);
            var posTotal = await posSales.SumAsync(sale => (decimal?)sale.TotalAmount, cancellationToken) ?? 0m;
            details.Add(new("فروش موفق صندوق (POS)", posCount.ToString("N0")));
            details.Add(new("جمع فروش موفق صندوق (POS)", FormatMoney(posTotal)));
            totalSales += posTotal;
            totalTransactions += posCount;
        }

        details.Add(new("جمع بخش‌های قابل دسترس", FormatMoney(totalSales)));
        details.Add(new("تعداد تراکنش‌های بخش‌های قابل دسترس", totalTransactions.ToString("N0")));

        var scope = canReadOnlineSales && canReadPosSales
            ? "پرداخت‌های آنلاین و فروش صندوق"
            : canReadOnlineSales ? "پرداخت‌های آنلاین؛ فروش صندوق به دلیل نداشتن مجوز وارد نشده است"
            : "فروش صندوق؛ پرداخت‌های آنلاین به دلیل نداشتن مجوز وارد نشده‌اند";

        return Reply(
            $"خلاصهٔ امروز بر اساس زمان پرداخت موفق (منطقهٔ زمانی {day.TimeZoneId}) آماده شد. دامنهٔ گزارش: {scope}. جمع مبالغ بخش‌های قابل دسترس {FormatMoney(totalSales)} است.",
            "live-data",
            "orders-db-and-pos-db",
            details,
            DateTimeOffset.UtcNow);
    }

    private static async Task<IResult> GetLowStockAsync(
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        IInventoryReportService inventoryReports,
        InventoryDbContext inventoryDb,
        CatalogDbContext catalogDb,
        CancellationToken cancellationToken)
    {
        if (!HasPermission(user, InventoryPermissions.Read))
        {
            return PermissionReply("inventory.read");
        }

        var summary = await inventoryReports.GetSummaryAsync(tenant.Id, cancellationToken);
        var details = new List<AssistantDetail>
        {
            new("تعداد ردیف‌های کم‌موجودی در شرکت فعال", summary.LowStockCount.ToString("N0"))
        };

        if (summary.LowStockCount == 0)
        {
            return Reply("طبق گزارش زندهٔ انبار شرکت فعال، در حال حاضر ردیف کم‌موجودی ثبت نشده است.",
                "live-data", "tenant-inventory-report", details, DateTimeOffset.UtcNow);
        }

        var lowStock = await inventoryReports.GetLowStockAsync(
            tenant.Id, null, 0, MaxDetails, cancellationToken);
        var canReadCatalog = HasPermission(user, ProductPermissions.Read);
        Dictionary<Guid, (string ProductName, string Sku)> labels = new();

        if (canReadCatalog && lowStock.Count > 0)
        {
            labels = await LoadTenantCatalogLabelsAsync(
                lowStock.Select(item => item.ProductVariantId),
                tenant.Id,
                inventoryDb,
                catalogDb,
                cancellationToken);
        }

        foreach (var item in lowStock)
        {
            var name = labels.TryGetValue(item.ProductVariantId, out var label)
                ? $"{label.ProductName} ({label.Sku})"
                : canReadCatalog
                    ? $"تنوع {item.ProductVariantId}"
                    : $"شناسهٔ تنوع {item.ProductVariantId}";

            details.Add(new(name,
                $"موجودی قابل‌دسترس: {item.AvailableQuantity:N0}؛ نقطهٔ سفارش: {item.ReorderPoint:N0}؛ رزرو: {item.ReservedQuantity:N0}"));
        }

        var suffix = summary.LowStockCount > lowStock.Count
            ? $" فقط {lowStock.Count} مورد اول نمایش داده شده است."
            : string.Empty;
        return Reply(
            $"در شرکت فعال {summary.LowStockCount:N0} ردیف انبار در سطح کم‌موجودی است.{suffix}",
            "live-data",
            "tenant-inventory-report",
            details,
            DateTimeOffset.UtcNow);
    }

    private static async Task<IResult> GetProductsAsync(
        string question,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        InventoryDbContext inventoryDb,
        CatalogDbContext catalogDb,
        CancellationToken cancellationToken)
    {
        if (!HasPermission(user, ProductPermissions.Read))
        {
            return PermissionReply("catalog.products.read");
        }

        var tenantVariantIds = await GetTenantVariantIdsAsync(inventoryDb, tenant.Id, cancellationToken);
        if (tenantVariantIds.Count == 0)
        {
            return Reply(
                "برای شرکت فعال، ردیف موجودی‌ای پیدا نشد که بتوان با اطمینان به آن کاتالوگ کالا را پیوند داد؛ بنابراین از کاتالوگ مشترک، فهرست عمومی ارائه نمی‌کنم.",
                "insufficient-data",
                "tenant-inventory-link",
                []);
        }

        var variantIds = tenantVariantIds.ToArray();
        var scoped = catalogDb.Products.AsNoTracking()
            .Include(product => product.Variants)
            .Where(product => product.Variants.Any(variant => variantIds.Contains(variant.Id)));

        if (IsProductCountQuestion(question))
        {
            var count = await scoped.Where(product => product.IsActive && product.IsPublished)
                .CountAsync(cancellationToken);
            return Reply(
                $"در کاتالوگ قابل اتصال به موجودی شرکت فعال، {count:N0} کالای فعال و منتشرشده پیدا شد. کالاهای بدون ردیف موجودی شرکت در این شمارش نیستند.",
                "live-data",
                "tenant-inventory-linked-catalog",
                [new("کالاهای فعال و منتشرشدهٔ قابل اتصال به شرکت فعال", count.ToString("N0"))],
                DateTimeOffset.UtcNow);
        }

        var terms = ExtractSearchTerms(question);
        if (terms.Length == 0)
        {
            return Reply("برای جستجوی کالا، بخشی از نام کالا یا SKU را وارد کنید؛ برای شمارش نیز بپرسید «چند محصول فعال داریم؟».",
                "insufficient-data", "tenant-inventory-linked-catalog", []);
        }

        foreach (var term in terms)
        {
            scoped = scoped.Where(product =>
                product.Name.Contains(term)
                || product.Sku.Contains(term)
                || product.Variants.Any(variant => variantIds.Contains(variant.Id) && variant.Sku.Contains(term)));
        }

        var products = await scoped.OrderBy(product => product.Name)
            .Take(MaxDetails)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return Reply(
                "در کالاهایی که به موجودی شرکت فعال قابل اتصال هستند، موردی مطابق این عبارت پیدا نشد. اگر نام دقیق‌تر، بخشی از نام یا SKU را وارد کنید، دوباره بررسی می‌کنم.",
                "live-data",
                "tenant-inventory-linked-catalog",
                [],
                DateTimeOffset.UtcNow);
        }

        var details = new List<AssistantDetail>();
        foreach (var product in products)
        {
            var tenantVariants = product.Variants.Where(variant => variantIds.Contains(variant.Id)).ToList();
            var skuList = string.Join(", ", tenantVariants.Select(variant => variant.Sku).Distinct().Take(5));
            var price = tenantVariants.Select(variant => variant.PriceOverride).FirstOrDefault(value => value > 0m);
            if (price <= 0m) price = product.Price;
            details.Add(new(product.Name, $"SKU: {(string.IsNullOrWhiteSpace(skuList) ? product.Sku : skuList)}؛ قیمت: {FormatMoney(price)}؛ وضعیت: {(product.IsActive && product.IsPublished ? "فعال و منتشرشده" : "غیرفعال یا منتشرنشده")}"));
        }

        var answerSuffix = products.Count == MaxDetails ? $" حداکثر {MaxDetails} نتیجه نمایش داده شد." : string.Empty;
        return Reply(
            $"{products.Count:N0} نتیجه در کاتالوگِ قابل اتصال به موجودی شرکت فعال پیدا شد.{answerSuffix}",
            "live-data",
            "tenant-inventory-linked-catalog",
            details,
            DateTimeOffset.UtcNow);
    }

    private static async Task<Dictionary<Guid, (string ProductName, string Sku)>> LoadTenantCatalogLabelsAsync(
        IEnumerable<Guid> requestedVariantIds,
        string tenantId,
        InventoryDbContext inventoryDb,
        CatalogDbContext catalogDb,
        CancellationToken cancellationToken)
    {
        var ownedVariantIds = await GetTenantVariantIdsAsync(inventoryDb, tenantId, cancellationToken);
        var targetIds = requestedVariantIds.Distinct().Where(ownedVariantIds.Contains).ToArray();
        if (targetIds.Length == 0) return new();

        var products = await catalogDb.Products.AsNoTracking()
            .Include(product => product.Variants)
            .Where(product => product.Variants.Any(variant => targetIds.Contains(variant.Id)))
            .Take(250)
            .ToListAsync(cancellationToken);

        return products
            .SelectMany(product => product.Variants
                .Where(variant => targetIds.Contains(variant.Id))
                .Select(variant => new KeyValuePair<Guid, (string ProductName, string Sku)>(
                    variant.Id, (product.Name, variant.Sku))))
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First().Value);
    }

    private static async Task<List<Guid>> GetTenantVariantIdsAsync(
        InventoryDbContext inventoryDb,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var stockItemIds = await inventoryDb.StockItems.AsNoTracking()
            .Where(stock => stock.TenantId == tenantId)
            .Select(stock => stock.ProductVariantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var warehouseIds = await inventoryDb.WarehouseStocks.AsNoTracking()
            .Where(stock => stock.TenantId == tenantId)
            .Select(stock => stock.ProductVariantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return stockItemIds.Concat(warehouseIds).Distinct().ToList();
    }

    private static IResult PermissionReply(string permission) => Reply(
        $"برای پاسخ به این پرسش، مجوز «{permission}» لازم است. دستیار اطلاعاتی خارج از مجوزهای حساب شما نمایش نمی‌دهد.",
        "permission-denied",
        "nexa-authorization",
        []);

    private static IResult Reply(
        string answer,
        string dataStatus,
        string source,
        IReadOnlyList<AssistantDetail> details,
        DateTimeOffset? dataAsOfUtc = null) => Results.Ok(new AssistantQueryResponse(
            answer, dataStatus, source, details, dataAsOfUtc, ExternalDataSent: false));

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        var granted = user.FindAll(PermissionClaims.ClaimType).Select(claim => claim.Value);
        return PermissionClaims.Satisfies(granted, permission);
    }

    private static (DateTime StartUtc, DateTime EndUtc, string TimeZoneId) GetBusinessDay(IConfiguration configuration)
    {
        var configuredId = configuration["Assistant:BusinessTimeZoneId"];
        if (string.IsNullOrWhiteSpace(configuredId)) configuredId = "Iran Standard Time";
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuredId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Local;
        }

        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var startLocal = DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        return (TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone), timeZone.Id);
    }

    private static bool IsActionRequest(string question) =>
        ContainsAny(question,
            "لغو سفارش", "حذف کالا", "حذف محصول", "تغییر قیمت", "تغییر وضعیت",
            "افزایش موجودی", "کاهش موجودی", "ثبت سفارش", "برگشت وجه", "استرداد وجه",
            "cancel order", "delete product", "change order status", "update stock",
            "change price", "place order", "refund");

    private static bool IsUnpaidOrderQuestion(string question) =>
        ContainsAny(question, "پرداخت نشده", "تسویه نشده", "منتظر پرداخت", "پرداخت نشده ها", "pending payment", "pendingpayment", "unpaid orders", "unpaid")
        || (question.Contains("سفارش") && question.Contains("پرداخت") && ContainsAny(question, "امروز", "today"));

    private static bool IsSalesSummaryQuestion(string question) =>
        ContainsAny(question, "خلاصه فروش", "فروش امروز", "جمع فروش", "میزان فروش", "پرداخت های موفق امروز", "پرداخت موفق امروز", "sales summary", "today's sales", "sales today", "revenue today", "total sales");

    private static bool IsProductQuestion(string question) =>
        ContainsAny(question, "محصول", "کالا", "sku", "قیمت", "کاتالوگ", "product") &&
        !ContainsAny(question, "کم موجودی", "موجودی کم", "رو به اتمام");

    private static bool IsProductCountQuestion(string question) =>
        ContainsAny(question, "چند کالا", "تعداد کالا", "چند محصول", "تعداد محصول", "تعداد محصولات", "چند تا محصول", "how many products", "product count");

    private static string[] ExtractSearchTerms(string question)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "محصول", "محصولات", "کالا", "کالای", "کالاها", "کالاهای", "قیمت", "چقدر", "چنده", "چند", "تا",
            "تعداد", "داریم", "دارم", "را", "رو", "از", "به", "درباره", "اطلاعات", "وضعیت", "موجودی", "کاتالوگ",
            "کد", "sku", "بگو", "بده", "پیدا", "کن", "جستجو", "لیست", "نمایش", "می‌شود", "میشه", "می‌شه",
            "لطفا", "لطفاً", "برای", "کدام", "کدوم", "داخل", "شرکت", "فعال", "چه", "است", "هست", "هستش", "من", "این"
        };

        return question.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => !stopWords.Contains(term) && term.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Replace('\u200c', ' ')
            .Replace('؟', ' ')
            .Replace('،', ' ')
            .Replace('؛', ' ')
            .Replace('۔', ' ');

        return string.Join(' ', normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string FormatMoney(decimal amount) => $"{amount:N0} ریال";

    private static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "در انتظار پرداخت",
        OrderStatus.Paid => "پرداخت‌شده",
        OrderStatus.Processing => "در حال پردازش",
        OrderStatus.Shipped => "ارسال‌شده",
        OrderStatus.Delivered => "تحویل‌شده",
        OrderStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };
}

public sealed record AssistantQueryRequest(string? Question);

public sealed record AssistantDetail(string Label, string Value);

public sealed record AssistantQueryResponse(
    string Answer,
    string DataStatus,
    string Source,
    IReadOnlyList<AssistantDetail> Details,
    DateTimeOffset? DataAsOfUtc,
    bool ExternalDataSent);
