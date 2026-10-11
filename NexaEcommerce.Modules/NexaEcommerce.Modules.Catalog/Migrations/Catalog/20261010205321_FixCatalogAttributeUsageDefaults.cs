using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class FixCatalogAttributeUsageDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ------------------------------------------------------------
            // 1. Repair invalid CatalogAttribute.DataType values.
            //
            // DataType:
            // 1 = Text
            // 2 = Integer
            // 3 = Decimal
            // 4 = Boolean
            // 5 = Date
            // 6 = DateTime
            // 7 = SingleSelect
            // 8 = MultiSelect
            //
            // For legacy variant attributes or attributes with catalog
            // values, SingleSelect is the safest compatible fallback.
            // ------------------------------------------------------------

            migrationBuilder.Sql(
                """
        UPDATE ca
        SET ca.DataType =
            CASE
                WHEN ca.IsVariantAttribute = 1
                     OR EXISTS
                     (
                         SELECT 1
                         FROM [Catalog].[CatalogAttributeValues] AS cav
                         WHERE cav.CatalogAttributeId = ca.Id
                           AND cav.IsDeleted = 0
                     )
                THEN 7
                ELSE 1
            END
        FROM [Catalog].[CatalogAttributes] AS ca
        WHERE ca.DataType < 1
           OR ca.DataType > 8;
        """);

            // ------------------------------------------------------------
            // 2. Repair invalid ProductAttribute.Role values.
            //
            // Role flags:
            // Filterable      = 1
            // Descriptive     = 2
            // VariantDefining = 4
            //
            // Existing variant mappings determine whether the attribute
            // participates in variants. Otherwise, use Descriptive.
            // Preserve the legacy IsFilterable setting when available.
            // ------------------------------------------------------------

            migrationBuilder.Sql(
                """
        UPDATE pa
        SET pa.Role =
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM [Catalog].[AttributeValues] AS av
                    INNER JOIN [Catalog].[VariantAttributeValues] AS vav
                        ON vav.AttributeValueId = av.Id
                    WHERE av.ProductAttributeId = pa.Id
                      AND av.IsDeleted = 0
                      AND vav.IsDeleted = 0
                )
                THEN 4
                ELSE 2
            END
            +
            CASE
                WHEN ISNULL(ca.IsFilterable, 0) = 1
                THEN 1
                ELSE 0
            END
        FROM [Catalog].[ProductAttributes] AS pa
        LEFT JOIN [Catalog].[CatalogAttributes] AS ca
            ON ca.Id = pa.CatalogAttributeId
        WHERE pa.Role = 0
           OR (pa.Role & ~7) <> 0;
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
