using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogueAndColour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vertical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymous_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vertical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "colour_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    owner_seller_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_proprietary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colour_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vertical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    vat_fraction = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    subtotal_inc_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_inc_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_inc_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_tiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vertical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    coverage_per_litre = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    coats_recommended = table.Column<int>(type: "integer", nullable: false),
                    is_tintable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sellers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    company_registration_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    vat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    default_fulfilment_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sellers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tint_bases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    uplift_per_litre_ex_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tint_bases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verticals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    hostname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verticals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "colours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colour_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    lrv = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    hue_family = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_discontinued = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colours", x => x.id);
                    table.ForeignKey(
                        name: "fk_colours_colour_systems_colour_system_id",
                        column: x => x.colour_system_id,
                        principalTable: "colour_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    sheen_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pack_litres = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    configuration_colour_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_colour_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    configuration_colour_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    configuration_colour_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    configuration_tint_base_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_tint_base_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    configuration_is_made_to_order = table.Column<bool>(type: "boolean", nullable: false),
                    configuration_resolved_price_ex_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    configuration_resolved_price_inc_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    configuration_vat_fraction = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    returnability = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    sheen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pack_litres = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    length_mm = table.Column<int>(type: "integer", nullable: true),
                    width_mm = table.Column<int>(type: "integer", nullable: true),
                    height_mm = table.Column<int>(type: "integer", nullable: true),
                    hazard_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sheen_uplift_factor = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "colour_availabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colour_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tint_base_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colourant_surcharge_ex_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_discontinued = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colour_availabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_colour_availabilities_colours_colour_id",
                        column: x => x.colour_id,
                        principalTable: "colours",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_colour_availabilities_tint_bases_tint_base_id",
                        column: x => x.tint_base_id,
                        principalTable: "tint_bases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_ex_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    fulfilment_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_offers_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_offers_sellers_seller_id",
                        column: x => x.seller_id,
                        principalTable: "sellers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    configuration_colour_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_colour_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    configuration_colour_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    configuration_colour_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    configuration_tint_base_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_tint_base_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    configuration_is_made_to_order = table.Column<bool>(type: "boolean", nullable: false),
                    configuration_resolved_price_ex_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    configuration_resolved_price_inc_vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    configuration_vat_fraction = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_cart_lines_carts_cart_id",
                        column: x => x.cart_id,
                        principalTable: "carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cart_lines_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_slug",
                table: "brands",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cart_lines_cart_id",
                table: "cart_lines",
                column: "cart_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_lines_offer_id",
                table: "cart_lines",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_carts_anonymous_id",
                table: "carts",
                column: "anonymous_id");

            migrationBuilder.CreateIndex(
                name: "ix_carts_customer_id",
                table: "carts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_vertical_id_slug",
                table: "categories",
                columns: new[] { "vertical_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_colour_availabilities_colour_id",
                table: "colour_availabilities",
                column: "colour_id");

            migrationBuilder.CreateIndex(
                name: "ix_colour_availabilities_product_id_colour_id",
                table: "colour_availabilities",
                columns: new[] { "product_id", "colour_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_colour_availabilities_tint_base_id",
                table: "colour_availabilities",
                column: "tint_base_id");

            migrationBuilder.CreateIndex(
                name: "ix_colour_systems_slug",
                table: "colour_systems",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_colours_colour_system_id_code",
                table: "colours",
                columns: new[] { "colour_system_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_colours_hue_family",
                table: "colours",
                column: "hue_family");

            migrationBuilder.CreateIndex(
                name: "ix_offers_product_variant_id",
                table: "offers",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_offers_seller_id_product_variant_id",
                table: "offers",
                columns: new[] { "seller_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_order_id",
                table: "order_lines",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_reference_number",
                table: "orders",
                column: "reference_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_tiers_name",
                table: "pricing_tiers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id_sheen_pack_litres",
                table: "product_variants",
                columns: new[] { "product_id", "sheen", "pack_litres" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_sku",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_vertical_id_slug",
                table: "products",
                columns: new[] { "vertical_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sellers_slug",
                table: "sellers",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tint_bases_seller_id_name",
                table: "tint_bases",
                columns: new[] { "seller_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verticals_hostname",
                table: "verticals",
                column: "hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verticals_slug",
                table: "verticals",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "cart_lines");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "colour_availabilities");

            migrationBuilder.DropTable(
                name: "order_lines");

            migrationBuilder.DropTable(
                name: "pricing_tiers");

            migrationBuilder.DropTable(
                name: "verticals");

            migrationBuilder.DropTable(
                name: "carts");

            migrationBuilder.DropTable(
                name: "offers");

            migrationBuilder.DropTable(
                name: "colours");

            migrationBuilder.DropTable(
                name: "tint_bases");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "sellers");

            migrationBuilder.DropTable(
                name: "colour_systems");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
