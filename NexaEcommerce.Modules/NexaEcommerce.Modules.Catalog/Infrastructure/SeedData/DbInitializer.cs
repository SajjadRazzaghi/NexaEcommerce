using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.SeedData;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        CatalogDbContext context)
    {
        // =====================================================
        // Apply migrations
        // =====================================================

        await context.Database.MigrateAsync();

        // =====================================================
        // Prevent duplicate seed
        // =====================================================

        if (await context.Products.AnyAsync())
            return;

        // =====================================================
        // BRANDS
        // =====================================================

        var samsung = new Brand(
            "سامسونگ",
            "برند معروف کره‌ای");

        var apple = new Brand(
            "اپل",
            "برند آمریکایی");

        var xiaomi = new Brand(
            "شیائومی",
            "برند چینی");

        var asus = new Brand(
            "ایسوس",
            "برند تایوانی");

        var bose = new Brand(
            "بوز",
            "برند تجهیزات صوتی");

        await context.Brands.AddRangeAsync(
            samsung,
            apple,
            xiaomi,
            asus,
            bose);

        // =====================================================
        // CATEGORIES
        // =====================================================

        var electronics =
            new Category("الکترونیک");

        var phones =
            new Category("موبایل و تبلت");

        var laptops =
            new Category("لپ‌تاپ");

        var accessories =
            new Category("لوازم جانبی");

        var headphones =
            new Category("هدفون و هندزفری");

        electronics.AddSubCategory(phones);
        electronics.AddSubCategory(laptops);
        electronics.AddSubCategory(accessories);

        accessories.AddSubCategory(headphones);

        await context.Categories.AddRangeAsync(
            electronics,
            phones,
            laptops,
            accessories,
            headphones);

        // =====================================================
        // PRODUCT 1
        // Samsung Galaxy S24
        // =====================================================

        var samsungS24 =
            new Product(
                "سامسونگ گلکسی S24",
                "S24-001",
                "samsung-galaxy-s24",
                25_000_000m,
                "IRR",
                "پرچمدار جدید سامسونگ با دوربین حرفه‌ای و عملکرد قدرتمند.");

        samsungS24.SetShortDescription(
            "گلکسی S24 با طراحی مدرن و عملکرد قدرتمند.");

        samsungS24.SetBrand(
            samsung.Id);

        samsungS24.SetFeatured(true);

        samsungS24.AddImage(
            "/images/products/samsung-s24-1.jpg",
            0,
            true);

        samsungS24.AddImage(
            "/images/products/samsung-s24-2.jpg",
            1,
            false);

        samsungS24.AddImage(
            "/images/products/samsung-s24-3.jpg",
            2,
            false);

        var s24Color =
            samsungS24.AddAttribute(
                "رنگ",
                "color");

        var s24Black =
            s24Color.AddValue(
                "black",
                "مشکی",
                "#000000");

        var s24White =
            s24Color.AddValue(
                "white",
                "سفید",
                "#FFFFFF");

        var s24Blue =
            s24Color.AddValue(
                "blue",
                "آبی",
                "#1E3A8A");

        var s24Storage =
            samsungS24.AddAttribute(
                "حافظه",
                "storage");

        var s24Storage256 =
            s24Storage.AddValue(
                "256GB",
                "۲۵۶ گیگابایت");

        var s24Storage512 =
            s24Storage.AddValue(
                "512GB",
                "۵۱۲ گیگابایت");

        var s24Black256 =
            samsungS24.AddVariant(
                "S24-BLK-256",
                25_000_000m);

        s24Black256.ChangeStock(10);
        s24Black256.AddAttributeValue(s24Black);
        s24Black256.AddAttributeValue(s24Storage256);

        var s24White256 =
            samsungS24.AddVariant(
                "S24-WHT-256",
                25_000_000m);

        s24White256.ChangeStock(8);
        s24White256.AddAttributeValue(s24White);
        s24White256.AddAttributeValue(s24Storage256);

        var s24Blue512 =
            samsungS24.AddVariant(
                "S24-BLU-512",
                29_000_000m);

        s24Blue512.ChangeStock(5);
        s24Blue512.AddAttributeValue(s24Blue);
        s24Blue512.AddAttributeValue(s24Storage512);

        // =====================================================
        // PRODUCT 2
        // iPhone 15 Pro Max
        // =====================================================

        var iphone =
            new Product(
                "اپل آیفون 15 پرو مکس",
                "IP15PM-001",
                "iphone-15-pro-max",
                35_000_000m,
                "IRR",
                "آیفون 15 پرو مکس با تراشه A17 Pro و بدنه تیتانیومی.");

        iphone.SetShortDescription(
            "آیفون 15 پرو مکس با بدنه تیتانیومی.");

        iphone.SetBrand(
            apple.Id);

        iphone.SetFeatured(true);

        iphone.AddImage(
            "/images/products/iphone-15-pro-max-1.jpg",
            0,
            true);

        iphone.AddImage(
            "/images/products/iphone-15-pro-max-2.jpg",
            1,
            false);

        var iphoneColor =
            iphone.AddAttribute(
                "رنگ",
                "color");

        var iphoneBlack =
            iphoneColor.AddValue(
                "black",
                "مشکی",
                "#000000");

        var iphoneNatural =
            iphoneColor.AddValue(
                "natural",
                "تیتانیومی طبیعی",
                "#A8A8A8");

        var iphoneBlue =
            iphoneColor.AddValue(
                "blue",
                "آبی",
                "#26354A");

        var iphoneStorage =
            iphone.AddAttribute(
                "حافظه",
                "storage");

        var iphone256 =
            iphoneStorage.AddValue(
                "256GB",
                "۲۵۶ گیگابایت");

        var iphone512 =
            iphoneStorage.AddValue(
                "512GB",
                "۵۱۲ گیگابایت");

        var iphoneBlack256 =
            iphone.AddVariant(
                "IP15PM-BLK-256",
                35_000_000m);

        iphoneBlack256.ChangeStock(6);
        iphoneBlack256.AddAttributeValue(iphoneBlack);
        iphoneBlack256.AddAttributeValue(iphone256);

        var iphoneNatural512 =
            iphone.AddVariant(
                "IP15PM-NAT-512",
                41_000_000m);

        iphoneNatural512.ChangeStock(3);
        iphoneNatural512.AddAttributeValue(iphoneNatural);
        iphoneNatural512.AddAttributeValue(iphone512);

        var iphoneBlue256 =
            iphone.AddVariant(
                "IP15PM-BLU-256",
                35_500_000m);

        iphoneBlue256.ChangeStock(4);
        iphoneBlue256.AddAttributeValue(iphoneBlue);
        iphoneBlue256.AddAttributeValue(iphone256);

        // =====================================================
        // PRODUCT 3
        // Xiaomi 14T Pro
        // =====================================================

        var xiaomi14T =
            new Product(
                "شیائومی 14T پرو",
                "X14T-001",
                "xiaomi-14t-pro",
                15_000_000m,
                "IRR",
                "گوشی قدرتمند شیائومی با نمایشگر باکیفیت و دوربین حرفه‌ای.");

        xiaomi14T.SetShortDescription(
            "شیائومی 14T پرو با عملکرد قدرتمند.");

        // FIX:
        // قبلاً اشتباهاً xiaomi14T.Id استفاده شده بود.
        xiaomi14T.SetBrand(
            xiaomi.Id);

        xiaomi14T.ApplyDiscount(15);

        xiaomi14T.SetFeatured(true);

        xiaomi14T.AddImage(
            "/images/products/xiaomi-14t-pro-1.jpg",
            0,
            true);

        xiaomi14T.AddImage(
            "/images/products/xiaomi-14t-pro-2.jpg",
            1,
            false);

        var xiaomiColor =
            xiaomi14T.AddAttribute(
                "رنگ",
                "color");

        var xiaomiBlack =
            xiaomiColor.AddValue(
                "black",
                "مشکی",
                "#000000");

        var xiaomiGreen =
            xiaomiColor.AddValue(
                "green",
                "سبز",
                "#3F5E4B");

        var xiaomiStorage =
            xiaomi14T.AddAttribute(
                "حافظه",
                "storage");

        var xiaomi256 =
            xiaomiStorage.AddValue(
                "256GB",
                "۲۵۶ گیگابایت");

        var xiaomi512 =
            xiaomiStorage.AddValue(
                "512GB",
                "۵۱۲ گیگابایت");

        var xiaomiBlack256 =
            xiaomi14T.AddVariant(
                "X14T-BLK-256",
                15_000_000m);

        xiaomiBlack256.ChangeStock(15);
        xiaomiBlack256.AddAttributeValue(xiaomiBlack);
        xiaomiBlack256.AddAttributeValue(xiaomi256);

        var xiaomiGreen512 =
            xiaomi14T.AddVariant(
                "X14T-GRN-512",
                18_000_000m);

        xiaomiGreen512.ChangeStock(7);
        xiaomiGreen512.AddAttributeValue(xiaomiGreen);
        xiaomiGreen512.AddAttributeValue(xiaomi512);

        // =====================================================
        // PRODUCT 4
        // ASUS ROG Zephyrus
        // =====================================================

        var asusLaptop =
            new Product(
                "لپ‌تاپ ایسوس ROG Zephyrus",
                "ROG-001",
                "asus-rog-zephyrus",
                45_000_000m,
                "IRR",
                "لپ‌تاپ گیمینگ قدرتمند ایسوس مناسب بازی و کارهای حرفه‌ای.");

        asusLaptop.SetShortDescription(
            "لپ‌تاپ گیمینگ قدرتمند با سخت‌افزار حرفه‌ای.");

        asusLaptop.SetBrand(
            asus.Id);

        asusLaptop.SetFeatured(true);

        asusLaptop.AddImage(
            "/images/products/asus-rog-1.jpg",
            0,
            true);

        asusLaptop.AddImage(
            "/images/products/asus-rog-2.jpg",
            1,
            false);

        var laptopColor =
            asusLaptop.AddAttribute(
                "رنگ",
                "color");

        var laptopBlack =
            laptopColor.AddValue(
                "black",
                "مشکی",
                "#111111");

        var laptopRam =
            asusLaptop.AddAttribute(
                "رم",
                "ram");

        var ram16 =
            laptopRam.AddValue(
                "16GB",
                "۱۶ گیگابایت");

        var ram32 =
            laptopRam.AddValue(
                "32GB",
                "۳۲ گیگابایت");

        var laptop16 =
            asusLaptop.AddVariant(
                "ROG-16GB",
                45_000_000m);

        laptop16.ChangeStock(5);
        laptop16.AddAttributeValue(laptopBlack);
        laptop16.AddAttributeValue(ram16);

        var laptop32 =
            asusLaptop.AddVariant(
                "ROG-32GB",
                52_000_000m);

        laptop32.ChangeStock(3);
        laptop32.AddAttributeValue(laptopBlack);
        laptop32.AddAttributeValue(ram32);

        // =====================================================
        // PRODUCT 5
        // Bose QC35
        // =====================================================

        var boseHeadphone =
            new Product(
                "هدفون بوز QC35",
                "BOSE-001",
                "bose-qc35",
                3_500_000m,
                "IRR",
                "هدفون بی‌سیم بوز با قابلیت حذف نویز فعال.");

        boseHeadphone.SetShortDescription(
            "هدفون بی‌سیم با کیفیت صدای عالی و حذف نویز.");

        boseHeadphone.SetBrand(
            bose.Id);

        boseHeadphone.SetFeatured(true);

        boseHeadphone.AddImage(
            "/images/products/bose-qc35-1.jpg",
            0,
            true);

        var boseColor =
            boseHeadphone.AddAttribute(
                "رنگ",
                "color");

        var boseBlack =
            boseColor.AddValue(
                "black",
                "مشکی",
                "#000000");

        var boseWhite =
            boseColor.AddValue(
                "white",
                "سفید",
                "#FFFFFF");

        var boseBlackVariant =
            boseHeadphone.AddVariant(
                "BOSE-QC35-BLK",
                3_500_000m);

        boseBlackVariant.ChangeStock(12);
        boseBlackVariant.AddAttributeValue(boseBlack);

        var boseWhiteVariant =
            boseHeadphone.AddVariant(
                "BOSE-QC35-WHT",
                3_500_000m);

        boseWhiteVariant.ChangeStock(6);
        boseWhiteVariant.AddAttributeValue(boseWhite);

        // =====================================================
        // PRODUCT 6
        // Samsung Galaxy A55
        // =====================================================

        var samsungA55 =
            new Product(
                "سامسونگ گلکسی A55",
                "A55-001",
                "samsung-galaxy-a55",
                12_500_000m,
                "IRR",
                "گوشی میان‌رده قدرتمند سامسونگ.");

        samsungA55.SetShortDescription(
            "گلکسی A55 انتخابی مناسب برای استفاده روزمره.");

        samsungA55.SetBrand(
            samsung.Id);

        samsungA55.ApplyDiscount(10);

        samsungA55.AddImage(
            "/images/products/samsung-a55-1.jpg",
            0,
            true);

        var a55Color =
            samsungA55.AddAttribute(
                "رنگ",
                "color");

        var a55Black =
            a55Color.AddValue(
                "black",
                "مشکی",
                "#000000");

        var a55Blue =
            a55Color.AddValue(
                "blue",
                "آبی",
                "#2563EB");

        var a55VariantBlack =
            samsungA55.AddVariant(
                "A55-BLK",
                12_500_000m);

        a55VariantBlack.ChangeStock(20);
        a55VariantBlack.AddAttributeValue(a55Black);

        var a55VariantBlue =
            samsungA55.AddVariant(
                "A55-BLU",
                12_500_000m);

        a55VariantBlue.ChangeStock(14);
        a55VariantBlue.AddAttributeValue(a55Blue);

        // =====================================================
        // PRODUCT 7
        // ASUS VivoBook
        // =====================================================

        var asusVivobook =
            new Product(
                "لپ‌تاپ ایسوس VivoBook",
                "VIVO-001",
                "asus-vivobook",
                32_000_000m,
                "IRR",
                "لپ‌تاپ سبک و قدرتمند ایسوس برای استفاده روزمره و کاری.");

        asusVivobook.SetShortDescription(
            "لپ‌تاپ سبک، سریع و مناسب کارهای روزمره.");

        asusVivobook.SetBrand(
            asus.Id);

        asusVivobook.AddImage(
            "/images/products/asus-vivobook-1.jpg",
            0,
            true);

        var vivoColor =
            asusVivobook.AddAttribute(
                "رنگ",
                "color");

        var vivoSilver =
            vivoColor.AddValue(
                "silver",
                "نقره‌ای",
                "#C0C0C0");

        var vivoRam =
            asusVivobook.AddAttribute(
                "رم",
                "ram");

        var vivo16 =
            vivoRam.AddValue(
                "16GB",
                "۱۶ گیگابایت");

        var vivoVariant =
            asusVivobook.AddVariant(
                "VIVO-16GB",
                32_000_000m);

        vivoVariant.ChangeStock(8);
        vivoVariant.AddAttributeValue(vivoSilver);
        vivoVariant.AddAttributeValue(vivo16);

        // =====================================================
        // PRODUCT 8
        // Bose QuietComfort Earbuds
        // =====================================================

        var boseEarbuds =
            new Product(
                "ایربادز بوز QuietComfort",
                "BOSE-EAR-001",
                "bose-quietcomfort-earbuds",
                5_800_000m,
                "IRR",
                "ایربادز بی‌سیم بوز با حذف نویز و کیفیت صدای بالا.");

        boseEarbuds.SetShortDescription(
            "ایربادز حرفه‌ای بوز برای موسیقی و مکالمه.");

        boseEarbuds.SetBrand(
            bose.Id);

        boseEarbuds.AddImage(
            "/images/products/bose-earbuds-1.jpg",
            0,
            true);

        var earbudsColor =
            boseEarbuds.AddAttribute(
                "رنگ",
                "color");

        var earbudsBlack =
            earbudsColor.AddValue(
                "black",
                "مشکی",
                "#000000");

        var earbudsWhite =
            earbudsColor.AddValue(
                "white",
                "سفید",
                "#FFFFFF");

        var earbudsBlackVariant =
            boseEarbuds.AddVariant(
                "BOSE-EAR-BLK",
                5_800_000m);

        earbudsBlackVariant.ChangeStock(10);
        earbudsBlackVariant.AddAttributeValue(
            earbudsBlack);

        var earbudsWhiteVariant =
            boseEarbuds.AddVariant(
                "BOSE-EAR-WHT",
                5_800_000m);

        earbudsWhiteVariant.ChangeStock(5);
        earbudsWhiteVariant.AddAttributeValue(
            earbudsWhite);

        // =====================================================
        // PRODUCT CATEGORIES
        // =====================================================

        samsungS24.ProductCategories.Add(
            new ProductCategory(
                samsungS24.Id,
                phones.Id));

        iphone.ProductCategories.Add(
            new ProductCategory(
                iphone.Id,
                phones.Id));

        xiaomi14T.ProductCategories.Add(
            new ProductCategory(
                xiaomi14T.Id,
                phones.Id));

        samsungA55.ProductCategories.Add(
            new ProductCategory(
                samsungA55.Id,
                phones.Id));

        asusLaptop.ProductCategories.Add(
            new ProductCategory(
                asusLaptop.Id,
                laptops.Id));

        asusVivobook.ProductCategories.Add(
            new ProductCategory(
                asusVivobook.Id,
                laptops.Id));

        boseHeadphone.ProductCategories.Add(
            new ProductCategory(
                boseHeadphone.Id,
                headphones.Id));

        boseEarbuds.ProductCategories.Add(
            new ProductCategory(
                boseEarbuds.Id,
                headphones.Id));

        // =====================================================
        // ADD PRODUCTS
        // =====================================================

        await context.Products.AddRangeAsync(
            samsungS24,
            iphone,
            xiaomi14T,
            asusLaptop,
            boseHeadphone,
            samsungA55,
            asusVivobook,
            boseEarbuds);

        // =====================================================
        // SAVE
        // =====================================================

        await context.SaveChangesAsync();
    }
}