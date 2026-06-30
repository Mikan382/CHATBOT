-- CreateSchema
CREATE SCHEMA IF NOT EXISTS "public";
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- CreateTable
CREATE TABLE "account" (
    "account_id" BIGSERIAL NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "email" VARCHAR(255),
    "is_verify" BOOLEAN NOT NULL,
    "name" VARCHAR(255),
    "password" VARCHAR(255),
    "phone" VARCHAR(255) NOT NULL,
    "role" VARCHAR(255),
    "status" VARCHAR(255),
    "username" VARCHAR(255),

    CONSTRAINT "account_pkey" PRIMARY KEY ("account_id")
);

-- CreateTable
CREATE TABLE "account_route" (
    "account_route_id" BIGSERIAL NOT NULL,
    "account_id" BIGINT NOT NULL,
    "route_id" BIGINT NOT NULL,

    CONSTRAINT "account_route_pkey" PRIMARY KEY ("account_route_id")
);

-- CreateTable
CREATE TABLE "address" (
    "address_id" BIGSERIAL NOT NULL,
    "address_name" VARCHAR(255) NOT NULL,
    "customer_id" BIGINT NOT NULL,

    CONSTRAINT "address_pkey" PRIMARY KEY ("address_id")
);

-- CreateTable
CREATE TABLE "auto_payment" (
    "auto_payment_id" BIGSERIAL NOT NULL,
    "amount" DECIMAL(38,2) NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "payment_code" VARCHAR(255) NOT NULL,
    "payment_purpose" VARCHAR(255) NOT NULL,

    CONSTRAINT "auto_payment_pkey" PRIMARY KEY ("auto_payment_id")
);

-- CreateTable
CREATE TABLE "bank_account" (
    "id" BIGSERIAL NOT NULL,
    "account_holder" VARCHAR(255) NOT NULL,
    "account_number" VARCHAR(255) NOT NULL,
    "bank_name" VARCHAR(255) NOT NULL,
    "is_proxy_payment" BOOLEAN NOT NULL,
    "is_revenue" BOOLEAN NOT NULL,

    CONSTRAINT "bank_account_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "customer" (
    "balance" DECIMAL(38,2),
    "customer_code" TEXT NOT NULL,
    "source" VARCHAR(255),
    "staff_id" BIGINT,
    "total_weight" DOUBLE PRECISION,
    "account_id" BIGINT NOT NULL,
    "total_amount" DECIMAL(38,2),
    "total_orders" INTEGER,

    CONSTRAINT "customer_pkey" PRIMARY KEY ("account_id")
);

-- CreateTable
CREATE TABLE "customer_voucher" (
    "customer_voucher_id" BIGSERIAL NOT NULL,
    "assigned_date" TIMESTAMP(6),
    "is_used" BOOLEAN NOT NULL,
    "used_date" TIMESTAMP(6),
    "uses_remaining" INTEGER,
    "customer_id" BIGINT NOT NULL,
    "voucher_id" BIGINT NOT NULL,

    CONSTRAINT "customer_voucher_pkey" PRIMARY KEY ("customer_voucher_id")
);

-- CreateTable
CREATE TABLE "destination" (
    "destination_id" BIGSERIAL NOT NULL,
    "destination_name" VARCHAR(255) NOT NULL,

    CONSTRAINT "destination_pkey" PRIMARY KEY ("destination_id")
);

-- CreateTable
CREATE TABLE "domestic" (
    "domestic_id" BIGSERIAL NOT NULL,
    "note" VARCHAR(255),
    "shipping_list" VARCHAR(255)[],
    "status" VARCHAR(255),
    "timestamp" TIMESTAMP(6) NOT NULL,
    "from_location_id" BIGINT,
    "location_id" BIGINT,
    "staff_id" BIGINT NOT NULL,
    "to_address_id" BIGINT,
    "to_location_id" BIGINT,
    "address" VARCHAR(255),
    "customer_id" BIGINT,
    "phone_number" VARCHAR(255),
    "ship_code" VARCHAR(255),
    "carrier" VARCHAR(20),
    "carrier_tracking_code" VARCHAR(255),
    "vnpost_tracking_code" VARCHAR(255),

    CONSTRAINT "domestic_pkey" PRIMARY KEY ("domestic_id")
);

-- CreateTable
CREATE TABLE "domestic_packing" (
    "packing_id" BIGINT NOT NULL,
    "domestic_id" BIGINT NOT NULL,

    CONSTRAINT "domestic_packing_pkey" PRIMARY KEY ("packing_id","domestic_id")
);

-- CreateTable
CREATE TABLE "draft_domestic" (
    "draft_domestic_id" BIGSERIAL NOT NULL,
    "vnpost_tracking_code" VARCHAR(255),
    "address" VARCHAR(255) NOT NULL,
    "phone_number" VARCHAR(255) NOT NULL,
    "weight" DOUBLE PRECISION,
    "customer_id" BIGINT NOT NULL,
    "note_tracking" VARCHAR(255),
    "ship_code" VARCHAR(255) NOT NULL,
    "staff_id" BIGINT NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "carrier" VARCHAR(255) NOT NULL,
    "status" VARCHAR(255) NOT NULL,
    "payment" BOOLEAN NOT NULL DEFAULT false,

    CONSTRAINT "draft_domestic_pkey" PRIMARY KEY ("draft_domestic_id")
);

-- CreateTable
CREATE TABLE "draft_domestic_shipping_list" (
    "draft_domestic_id" BIGINT NOT NULL,
    "shipping_list" VARCHAR(255),
    "id" BIGSERIAL NOT NULL
);

-- CreateTable
CREATE TABLE "expense_request" (
    "expense_request_id" BIGSERIAL NOT NULL,
    "bank_info" VARCHAR(255),
    "cancel_reason" VARCHAR(255),
    "created_at" TIMESTAMP(6) NOT NULL,
    "department" VARCHAR(255),
    "description" VARCHAR(255) NOT NULL,
    "invoice_image" VARCHAR(255),
    "note" VARCHAR(255),
    "payment_method" VARCHAR(255) NOT NULL,
    "quantity" INTEGER NOT NULL,
    "status" VARCHAR(255) NOT NULL,
    "total_amount" DECIMAL(38,2) NOT NULL,
    "transfer_image" VARCHAR(255),
    "unit_price" DECIMAL(38,2) NOT NULL,
    "vat_info" VARCHAR(255),
    "vat_status" VARCHAR(255) NOT NULL,
    "approver_id" BIGINT,
    "requester_id" BIGINT NOT NULL,

    CONSTRAINT "expense_request_pkey" PRIMARY KEY ("expense_request_id")
);

-- CreateTable
CREATE TABLE "feedback" (
    "feedback_id" BIGSERIAL NOT NULL,
    "comment" VARCHAR(255) NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "rating" INTEGER NOT NULL,
    "order_id" BIGINT NOT NULL,

    CONSTRAINT "feedback_pkey" PRIMARY KEY ("feedback_id")
);

-- CreateTable
CREATE TABLE "flight_shipment" (
    "flight_shipment_id" BIGSERIAL NOT NULL,
    "air_freight_cost" DECIMAL(38,2),
    "air_freight_paid" BOOLEAN NOT NULL,
    "air_freight_paid_date" TIMESTAMP(6),
    "airport_shipping_cost" DECIMAL(38,2),
    "arrival_date" TIMESTAMP(6),
    "awb_file_path" VARCHAR(255),
    "created_at" TIMESTAMP(6),
    "customs_clearance_cost" DECIMAL(38,2),
    "customs_paid" BOOLEAN NOT NULL,
    "customs_paid_date" TIMESTAMP(6),
    "flight_code" VARCHAR(255) NOT NULL,
    "gross_profit" DECIMAL(38,2),
    "invoice_file_path" VARCHAR(255),
    "origin_cost_per_kg" DECIMAL(38,2),
    "other_costs" DECIMAL(38,2),
    "status" VARCHAR(255),
    "total_cost" DECIMAL(38,2),
    "total_volume_weight" DECIMAL(38,2),
    "staff_id" BIGINT NOT NULL,
    "updated_at" TIMESTAMP(6),
    "export_license_path" VARCHAR(255),
    "packing_list_path" VARCHAR(255),
    "single_invoice_path" VARCHAR(255),
    "flight_name" VARCHAR(255),

    CONSTRAINT "flight_shipment_pkey" PRIMARY KEY ("flight_shipment_id")
);

-- CreateTable
CREATE TABLE "marketing_media" (
    "media_id" BIGSERIAL NOT NULL,
    "created_date" TIMESTAMP(6),
    "description" VARCHAR(255),
    "end_date" TIMESTAMP(6),
    "link_url" VARCHAR(255),
    "media_url" VARCHAR(255),
    "position" VARCHAR(255),
    "sorting" INTEGER NOT NULL,
    "start_date" TIMESTAMP(6),
    "status" VARCHAR(255),
    "title" VARCHAR(255),
    "staff_id" BIGINT NOT NULL,

    CONSTRAINT "marketing_media_pkey" PRIMARY KEY ("media_id")
);

-- CreateTable
CREATE TABLE "order_links" (
    "link_id" BIGSERIAL NOT NULL,
    "classify" VARCHAR(255),
    "extra_charge" DECIMAL(38,2),
    "final_price_vnd" DECIMAL(38,2),
    "group_tag" VARCHAR(255),
    "note" VARCHAR(255),
    "price_web" DECIMAL(38,2),
    "product_link" TEXT,
    "product_name" VARCHAR(255),
    "purchase_fee" DECIMAL(38,2),
    "purchase_image" VARCHAR(255),
    "quantity" INTEGER NOT NULL,
    "ship_web" DECIMAL(38,2),
    "shipment_code" TEXT,
    "status" VARCHAR(255),
    "total_web" DECIMAL(38,2),
    "tracking_code" VARCHAR(255) NOT NULL,
    "website" VARCHAR(255),
    "order_id" BIGINT NOT NULL,
    "partial_shipment_id" BIGINT,
    "product_type_id" BIGINT,
    "purchase_id" BIGINT,
    "warehouse_id" BIGINT,

    CONSTRAINT "order_links_pkey" PRIMARY KEY ("link_id")
);

-- CreateTable
CREATE TABLE "order_process_log" (
    "log_id" BIGSERIAL NOT NULL,
    "action" VARCHAR(255),
    "action_code" VARCHAR(255) NOT NULL,
    "role_at_time" VARCHAR(255),
    "timestamp" TIMESTAMP(6) NOT NULL,
    "order_id" BIGINT,
    "staff_id" BIGINT,

    CONSTRAINT "order_process_log_pkey" PRIMARY KEY ("log_id")
);

-- CreateTable
CREATE TABLE "orders" (
    "order_id" BIGSERIAL NOT NULL,
    "check_required" BOOLEAN NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "exchange_rate" DECIMAL(38,2),
    "final_price_order" DECIMAL(38,2),
    "image_check" VARCHAR(255)[],
    "leftover_money" DECIMAL(38,2),
    "order_code" VARCHAR(255) NOT NULL,
    "order_type" VARCHAR(255),
    "pinned_at" TIMESTAMP(6),
    "price_before_fee" DECIMAL(38,2),
    "price_ship" DECIMAL(38,2),
    "status" VARCHAR(255),
    "address_id" BIGINT,
    "customer_id" BIGINT NOT NULL,
    "destination_id" BIGINT NOT NULL,
    "route_id" BIGINT NOT NULL,
    "staff_id" BIGINT NOT NULL,
    "voucher_applied_id" BIGINT,
    "payment_after_auction" DECIMAL(38,2),
    "note" TEXT,

    CONSTRAINT "orders_pkey" PRIMARY KEY ("order_id")
);

-- CreateTable
CREATE TABLE "otp" (
    "id" BIGSERIAL NOT NULL,
    "code" VARCHAR(255),
    "expiration" TIMESTAMP(6),
    "account_id" BIGINT NOT NULL,

    CONSTRAINT "otp_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "packing" (
    "packing_id" BIGSERIAL NOT NULL,
    "flight_code" VARCHAR(255),
    "packed_date" TIMESTAMP(6) NOT NULL,
    "packing_code" VARCHAR(255) NOT NULL,
    "packing_list" VARCHAR(255)[],
    "status" VARCHAR(255),
    "destination_id" BIGINT NOT NULL,
    "staff_id" BIGINT NOT NULL,
    "route_code" VARCHAR(255),
    "status_flight" BOOLEAN,
    "fly_time" TIMESTAMP(6),
    "domestic_id" BIGINT,

    CONSTRAINT "packing_pkey" PRIMARY KEY ("packing_id")
);

-- CreateTable
CREATE TABLE "packing_domestic" (
    "packing_id" BIGINT NOT NULL,
    "domestic_id" BIGINT NOT NULL,

    CONSTRAINT "packing_domestic_pkey" PRIMARY KEY ("packing_id","domestic_id")
);

-- CreateTable
CREATE TABLE "packing_related_orders" (
    "packing_packing_id" BIGINT NOT NULL,
    "related_orders_order_id" BIGINT NOT NULL,

    CONSTRAINT "packing_related_orders_pkey" PRIMARY KEY ("packing_packing_id","related_orders_order_id")
);

-- CreateTable
CREATE TABLE "partial_shipment" (
    "id" BIGSERIAL NOT NULL,
    "note" VARCHAR(255),
    "partial_amount" DECIMAL(38,2),
    "shipment_date" TIMESTAMP(6),
    "status" VARCHAR(255),
    "order_id" BIGINT NOT NULL,
    "payment_id" BIGINT,
    "staff_id" BIGINT,
    "collect_weight" DECIMAL(38,2),

    CONSTRAINT "partial_shipment_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "payment" (
    "payment_id" BIGSERIAL NOT NULL,
    "action_at" TIMESTAMP(6) NOT NULL,
    "amount" DECIMAL(38,2) NOT NULL,
    "collected_amount" DECIMAL(38,2) NOT NULL,
    "content" TEXT,
    "deposit_percent" INTEGER,
    "is_merged_payment" BOOLEAN NOT NULL,
    "payment_code" VARCHAR(255) NOT NULL,
    "payment_type" VARCHAR(255),
    "qr_code" VARCHAR(255),
    "status" VARCHAR(255),
    "customer_id" BIGINT NOT NULL,
    "order_id" BIGINT,
    "staff_id" BIGINT NOT NULL,
    "purpose" VARCHAR(255),
    "collect_weight" DECIMAL,
    "paid_time" TIMESTAMP(6),

    CONSTRAINT "payment_pkey" PRIMARY KEY ("payment_id")
);

-- CreateTable
CREATE TABLE "payment_orders" (
    "payment_id" BIGINT NOT NULL,
    "order_id" BIGINT NOT NULL,

    CONSTRAINT "payment_orders_pkey" PRIMARY KEY ("payment_id","order_id")
);

-- CreateTable
CREATE TABLE "product_type" (
    "product_type_id" BIGSERIAL NOT NULL,
    "is_fee" BOOLEAN NOT NULL,
    "product_type_name" VARCHAR(255) NOT NULL,

    CONSTRAINT "product_type_pkey" PRIMARY KEY ("product_type_id")
);

-- CreateTable
CREATE TABLE "purchases" (
    "purchase_id" BIGSERIAL NOT NULL,
    "final_price_order" DECIMAL(38,2),
    "note" VARCHAR(255),
    "purchase_code" VARCHAR(255),
    "purchase_image" VARCHAR(255),
    "purchase_time" TIMESTAMP(6),
    "order_id" BIGINT NOT NULL,
    "staff_id" BIGINT NOT NULL,
    "is_purchased" BOOLEAN,
    "exchange_rate" DECIMAL(38,2),
    "invoice" VARCHAR(255),

    CONSTRAINT "purchases_pkey" PRIMARY KEY ("purchase_id")
);

-- CreateTable
CREATE TABLE "repack" (
    "repack_id" BIGSERIAL NOT NULL,
    "completed_at" TIMESTAMP(6),
    "created_at" TIMESTAMP(6) NOT NULL,
    "repack_code" VARCHAR(255) NOT NULL,
    "repack_list" VARCHAR(255)[],
    "status" VARCHAR(255) NOT NULL,
    "customer_id" BIGINT NOT NULL,
    "resulting_packing_id" BIGINT,
    "staff_id" BIGINT NOT NULL,

    CONSTRAINT "repack_pkey" PRIMARY KEY ("repack_id")
);

-- CreateTable
CREATE TABLE "route" (
    "route_id" BIGSERIAL NOT NULL,
    "exchange_rate" DECIMAL(38,2),
    "name" VARCHAR(255),
    "note" VARCHAR(255),
    "ship_time" VARCHAR(255),
    "unit_buying_price" DECIMAL(38,2),
    "unit_deposit_price" DECIMAL(38,2),
    "difference_rate" DECIMAL(38,2),
    "is_update_auto" BOOLEAN NOT NULL DEFAULT true,
    "min_weight" DECIMAL(38,2),

    CONSTRAINT "route_pkey" PRIMARY KEY ("route_id")
);

-- CreateTable
CREATE TABLE "route_exchange_rate" (
    "route_exchange_rate_id" BIGSERIAL NOT NULL,
    "end_date" DATE,
    "exchange_rate" DECIMAL(38,2) NOT NULL,
    "start_date" DATE NOT NULL,
    "route_id" BIGINT NOT NULL,
    "note" VARCHAR(255),

    CONSTRAINT "route_exchange_rate_pkey" PRIMARY KEY ("route_exchange_rate_id")
);

-- CreateTable
CREATE TABLE "route_warehouse_locations" (
    "route_id" BIGINT NOT NULL,
    "location_id" BIGINT NOT NULL,

    CONSTRAINT "route_warehouse_locations_pkey" PRIMARY KEY ("route_id","location_id")
);

-- CreateTable
CREATE TABLE "shipment_tracking" (
    "shipment_id" BIGSERIAL NOT NULL,
    "current_location" VARCHAR(255) NOT NULL,
    "status" VARCHAR(255),
    "timestamp" TIMESTAMP(6) NOT NULL,
    "order_id" BIGINT NOT NULL,

    CONSTRAINT "shipment_tracking_pkey" PRIMARY KEY ("shipment_id")
);

-- CreateTable
CREATE TABLE "staff" (
    "department" VARCHAR(255),
    "location" VARCHAR(255),
    "staff_code" VARCHAR(255) NOT NULL,
    "account_id" BIGINT NOT NULL,
    "warehouse_location_id" BIGINT,
    "can_approve_expenses" BOOLEAN,
    "can_request_expenses" BOOLEAN,

    CONSTRAINT "staff_pkey" PRIMARY KEY ("account_id")
);

-- CreateTable
CREATE TABLE "voucher" (
    "voucher_id" BIGSERIAL NOT NULL,
    "assign_type" VARCHAR(255) NOT NULL,
    "code" VARCHAR(255) NOT NULL,
    "description" VARCHAR(255),
    "end_date" TIMESTAMP(6),
    "max_uses" INTEGER,
    "min_order_value" DECIMAL(38,2),
    "start_date" TIMESTAMP(6),
    "threshold_amount" DOUBLE PRECISION,
    "type" VARCHAR(255) NOT NULL,
    "value" DECIMAL(38,2) NOT NULL,

    CONSTRAINT "voucher_pkey" PRIMARY KEY ("voucher_id")
);

-- CreateTable
CREATE TABLE "voucher_route" (
    "voucher_id" BIGINT NOT NULL,
    "route_id" BIGINT NOT NULL,

    CONSTRAINT "voucher_route_pkey" PRIMARY KEY ("voucher_id","route_id")
);

-- CreateTable
CREATE TABLE "warehouse" (
    "warehouse_id" BIGSERIAL NOT NULL,
    "created_at" TIMESTAMP(6) NOT NULL,
    "dim" DOUBLE PRECISION,
    "height" DOUBLE PRECISION,
    "image" VARCHAR(255),
    "image_check" VARCHAR(255),
    "length" DOUBLE PRECISION,
    "net_weight" DOUBLE PRECISION,
    "status" VARCHAR(255),
    "tracking_code" TEXT NOT NULL,
    "weight" DOUBLE PRECISION,
    "width" DOUBLE PRECISION,
    "location_id" BIGINT NOT NULL,
    "order_id" BIGINT NOT NULL,
    "packing_id" BIGINT,
    "purchase_id" BIGINT,
    "staff_id" BIGINT NOT NULL,
    "flight_shipment_id" BIGINT,
    "arrival_time" TIMESTAMP(6),
    "delivery_time" TIMESTAMP(6),
    "dispatch_time" TIMESTAMP(6),
    "updated_at" TIMESTAMP(6),
    "repack_id" BIGINT,

    CONSTRAINT "warehouse_pkey" PRIMARY KEY ("warehouse_id")
);

-- CreateTable
CREATE TABLE "warehouse_location" (
    "location_id" BIGSERIAL NOT NULL,
    "address" VARCHAR(255),
    "country" VARCHAR(255),
    "is_active" BOOLEAN NOT NULL,
    "name" VARCHAR(255),
    "province" VARCHAR(255),
    "type" SMALLINT,

    CONSTRAINT "warehouse_location_pkey" PRIMARY KEY ("location_id")
);

-- CreateTable
CREATE TABLE "websites" (
    "website_id" BIGSERIAL NOT NULL,
    "website_name" VARCHAR(255),

    CONSTRAINT "websites_pkey" PRIMARY KEY ("website_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "uk_q0uja26qgu1atulenwup9rxyr" ON "account"("email");

-- CreateIndex
CREATE UNIQUE INDEX "uk_gex1lmaqpg0ir5g1f5eftyaa1" ON "account"("username");

-- CreateIndex
CREATE UNIQUE INDEX "uk_mb8kv2x9143o96jgxbv6mahcq" ON "bank_account"("account_number");

-- CreateIndex
CREATE UNIQUE INDEX "uk_114lxb57nwilrwigcoi6nm3ln" ON "customer"("customer_code");

-- CreateIndex
CREATE INDEX "idx_customer_code_trgm" ON "customer" USING GIN ("customer_code" gin_trgm_ops);

-- CreateIndex
CREATE INDEX "idx_draft_domestic_carrier" ON "draft_domestic"("carrier");

-- CreateIndex
CREATE INDEX "idx_draft_domestic_staff" ON "draft_domestic"("staff_id");

-- CreateIndex
CREATE INDEX "idx_draft_domestic_status" ON "draft_domestic"("status");

-- CreateIndex
CREATE INDEX "idx_draft_domestic_shipping_domestic" ON "draft_domestic_shipping_list"("draft_domestic_id");

-- CreateIndex
CREATE INDEX "idx_draft_domestic_shipping_value" ON "draft_domestic_shipping_list"("shipping_list");

-- CreateIndex
CREATE INDEX "idx_shipping_list_value" ON "draft_domestic_shipping_list"("shipping_list");

-- CreateIndex
CREATE UNIQUE INDEX "uk_bx8xib8nsamd39qx9fea6x1pi" ON "feedback"("order_id");

-- CreateIndex
CREATE UNIQUE INDEX "uk_2blwi367ivdxaoashdxblqty1" ON "flight_shipment"("flight_code");

-- CreateIndex
CREATE INDEX "idx_ol_purchase_shipment" ON "order_links"("purchase_id", "shipment_code");

-- CreateIndex
CREATE INDEX "idx_ol_purchase_status" ON "order_links"("purchase_id", "status");

-- CreateIndex
CREATE INDEX "idx_orders_code_trgm" ON "orders" USING GIN ("order_code" gin_trgm_ops);

-- CreateIndex
CREATE INDEX "idx_orders_route_status" ON "orders"("route_id", "status");

-- CreateIndex
CREATE INDEX "idx_orders_staff_status" ON "orders"("staff_id", "status");

-- CreateIndex
CREATE INDEX "idx_orders_status" ON "orders"("status");

-- CreateIndex
CREATE INDEX "idx_purchases_is_purchased" ON "purchases"("is_purchased");

-- CreateIndex
CREATE UNIQUE INDEX "uk_jie9sis1js7xr2cwe518xot00" ON "repack"("repack_code");

-- CreateIndex
CREATE UNIQUE INDEX "uk_5kgtet4nu2iqccmrpkb2sj27a" ON "repack"("resulting_packing_id");

-- CreateIndex
CREATE UNIQUE INDEX "uk_tkv6oqgei31ht6h0jsoj61008" ON "staff"("staff_code");

-- CreateIndex
CREATE UNIQUE INDEX "uk_pvh1lqheshnjoekevvwla03xn" ON "voucher"("code");

-- AddForeignKey
ALTER TABLE "account_route" ADD CONSTRAINT "fk135c5ac5pqm32dijxfrqw83ud" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "account_route" ADD CONSTRAINT "fkmvh5hba5pkyeg3f63y1orbvj2" FOREIGN KEY ("account_id") REFERENCES "account"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "address" ADD CONSTRAINT "fk93c3js0e22ll1xlu21nvrhqgg" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "customer" ADD CONSTRAINT "fkn9x2k8svpxj3r328iy1rpur83" FOREIGN KEY ("account_id") REFERENCES "account"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "customer_voucher" ADD CONSTRAINT "fkanegic8piajdx0lkwampaausi" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "customer_voucher" ADD CONSTRAINT "fkge2rx5y4tx4ucsgvhx9f5blbq" FOREIGN KEY ("voucher_id") REFERENCES "voucher"("voucher_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fk13lbi9lq0txgvt8102a37slwn" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fk46u7k507rnlssfoxhx65nn47x" FOREIGN KEY ("to_address_id") REFERENCES "address"("address_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fk5j3acqg7iky5tgy896faguwl6" FOREIGN KEY ("to_location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fkao3tgjihi7olurpds2siktw11" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fkpkvpyqmm3w1rrj97t5plxpjyd" FOREIGN KEY ("location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "fkscfd2uvbpk6mh8t28533dj2sf" FOREIGN KEY ("from_location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic_packing" ADD CONSTRAINT "fk3dpgkuskc6rd74f9fe8q98ss" FOREIGN KEY ("domestic_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "domestic_packing" ADD CONSTRAINT "fkcc7nd6pmdrk16vjlin5mfb45s" FOREIGN KEY ("packing_id") REFERENCES "packing"("packing_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "draft_domestic" ADD CONSTRAINT "fk4dhbyfdyksiseftqbxsbi8xrx" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "draft_domestic" ADD CONSTRAINT "fkp8qhjymaf94163gbtrebyy8mc" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "draft_domestic_shipping_list" ADD CONSTRAINT "fkrpktj0wjgkdnlr7bmdp3bybba" FOREIGN KEY ("draft_domestic_id") REFERENCES "draft_domestic"("draft_domestic_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "expense_request" ADD CONSTRAINT "fkj8rl7jb5r99f8n0gqln4kg9sy" FOREIGN KEY ("approver_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "expense_request" ADD CONSTRAINT "fknho3xaxgyh43w9wrwg9ovhr32" FOREIGN KEY ("requester_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "feedback" ADD CONSTRAINT "fk66tdec0kx8px7cc7xbw3ffx8h" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "flight_shipment" ADD CONSTRAINT "fkb70p3jjlit9yxc08acifgm7lr" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "marketing_media" ADD CONSTRAINT "fkow2ygd8xnlypfjqt3hwdujamn" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "fk7bsgs23x35xbi5i75n5tb1o90" FOREIGN KEY ("product_type_id") REFERENCES "product_type"("product_type_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "fk897608b72pfbwovbdqv6kf9ni" FOREIGN KEY ("warehouse_id") REFERENCES "warehouse"("warehouse_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "fk96k3s3f8p4jufb1rdt0yu8luc" FOREIGN KEY ("partial_shipment_id") REFERENCES "partial_shipment"("id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "fkmoe4fe7qqr4oxd5442qw1xvff" FOREIGN KEY ("purchase_id") REFERENCES "purchases"("purchase_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "fks2vmf4smcbu0kl2hgj5if3a4r" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_process_log" ADD CONSTRAINT "fkcflkn9s3511yftndr1hange3t" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_process_log" ADD CONSTRAINT "fkqhsalm234n3f4kohek20i5620" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fk2dr2r3tj18ho0fto8adl0l3lp" FOREIGN KEY ("voucher_applied_id") REFERENCES "customer_voucher"("customer_voucher_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fk39eskd19qnrtvyg3h8yxhg3b8" FOREIGN KEY ("destination_id") REFERENCES "destination"("destination_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fk4ery255787xl56k025fyxrqe9" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fk624gtjin3po807j3vix093tlf" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fkeop7en0d481ppxbnglcmxd5u9" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "fkf5464gxwc32ongdvka2rtvw96" FOREIGN KEY ("address_id") REFERENCES "address"("address_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "otp" ADD CONSTRAINT "fk70ucdaogffhlarex8tsquh1ys" FOREIGN KEY ("account_id") REFERENCES "account"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing" ADD CONSTRAINT "fkafk4qgm4o0hggf31ridd1veaj" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing" ADD CONSTRAINT "fkciu28dvdeym49pemna3vyd7vr" FOREIGN KEY ("destination_id") REFERENCES "destination"("destination_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing" ADD CONSTRAINT "fki30mwxjek85mbhlk9r9hgfoyn" FOREIGN KEY ("domestic_id") REFERENCES "domestic"("domestic_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing_domestic" ADD CONSTRAINT "fklwq6i1cptkedkewhuaka99j4t" FOREIGN KEY ("packing_id") REFERENCES "packing"("packing_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing_domestic" ADD CONSTRAINT "fkqx9vywhrogc54xd6bkxkv9010" FOREIGN KEY ("domestic_id") REFERENCES "domestic"("domestic_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing_related_orders" ADD CONSTRAINT "fk2jjf2tq9egxjxjv353nn1epn7" FOREIGN KEY ("packing_packing_id") REFERENCES "packing"("packing_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "packing_related_orders" ADD CONSTRAINT "fk2l44bm6q1x44up82avoureukp" FOREIGN KEY ("related_orders_order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "partial_shipment" ADD CONSTRAINT "fk19cnh66ggagkcddii1sms5702" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "partial_shipment" ADD CONSTRAINT "fk8l44qb1ay7rh29pugcb6tp41c" FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "partial_shipment" ADD CONSTRAINT "fkkw9r7e8hhqu7yh6ox34pexnxi" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "payment" ADD CONSTRAINT "fkby2skjf3ov608yb6nm16b49lg" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "payment" ADD CONSTRAINT "fklouu98csyullos9k25tbpk4va" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "payment" ADD CONSTRAINT "fkr2ky59817r20r8fmb030pgw5p" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "payment_orders" ADD CONSTRAINT "fk68j4m4bgn2xx64m1mdou26322" FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "payment_orders" ADD CONSTRAINT "fkohtixrr5nsywabsqddlhdmx78" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "purchases" ADD CONSTRAINT "fk1uf4rjnbind4fuxbeqwqs2hnh" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "purchases" ADD CONSTRAINT "fkggo75366vlws6fp6684tmem1a" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "repack" ADD CONSTRAINT "fk40wdn2tcys3to4euw5wixd7pj" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "repack" ADD CONSTRAINT "fkl0wpmf8eb4d0ms1vpbhi36tmu" FOREIGN KEY ("resulting_packing_id") REFERENCES "packing"("packing_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "repack" ADD CONSTRAINT "fksvrs8e60oid33agpa45nrppq8" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "route_exchange_rate" ADD CONSTRAINT "fk6a5vj9pqsyusua403i6ggdj50" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "route_warehouse_locations" ADD CONSTRAINT "fk5rs5cbe5bwg0b6up82p4lkvd3" FOREIGN KEY ("location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "route_warehouse_locations" ADD CONSTRAINT "fkj64193hm2au94noyx8fedmsaq" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "shipment_tracking" ADD CONSTRAINT "fk1yjlc4kh5b84md5yx0v9pwffp" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "staff" ADD CONSTRAINT "fk2wagv5ymnx0n1of5lcw371d02" FOREIGN KEY ("warehouse_location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "staff" ADD CONSTRAINT "fks9jl798sgmtrl79dm4svocvaw" FOREIGN KEY ("account_id") REFERENCES "account"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "voucher_route" ADD CONSTRAINT "fkcxsd20covc9dig0ks3r1h4kci" FOREIGN KEY ("voucher_id") REFERENCES "voucher"("voucher_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "voucher_route" ADD CONSTRAINT "fkmobiw7ihovpwhyu42i7b6w0ol" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fk3o92h4r0bldtkdtn3k6g15cv7" FOREIGN KEY ("flight_shipment_id") REFERENCES "flight_shipment"("flight_shipment_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fka3a3p9ln20gu24hfnrwt8bmss" FOREIGN KEY ("location_id") REFERENCES "warehouse_location"("location_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fkaj303s761d23xxfwi3u1npkc0" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fkek35o9i7st6fh25acts395b8g" FOREIGN KEY ("repack_id") REFERENCES "repack"("repack_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fkmhj18fip10udlo0omk7sm5nwx" FOREIGN KEY ("packing_id") REFERENCES "packing"("packing_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fkq6lwlp0nr8d1s5x37no952nlr" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "fks4b37rtdipo83vk7k9bc9mi4d" FOREIGN KEY ("purchase_id") REFERENCES "purchases"("purchase_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

