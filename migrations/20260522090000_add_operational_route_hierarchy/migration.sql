ALTER TABLE "route"
  ADD COLUMN "parent_route_id" BIGINT;

ALTER TABLE "orders"
  ADD COLUMN "operational_route_id" BIGINT;

CREATE INDEX "idx_route_parent_route_id"
  ON "route"("parent_route_id");

CREATE INDEX "idx_orders_operational_route_id"
  ON "orders"("operational_route_id");

ALTER TABLE "route"
  ADD CONSTRAINT "fk_route_parent_route"
  FOREIGN KEY ("parent_route_id")
  REFERENCES "route"("route_id")
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

ALTER TABLE "orders"
  ADD CONSTRAINT "fk_orders_operational_route"
  FOREIGN KEY ("operational_route_id")
  REFERENCES "route"("route_id")
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

-- Create one operational child route only when the parent route has a single
-- unambiguous active warehouse pair. Routes with multiple locations need a
-- deliberate child-route setup instead of an inferred cartesian product.
WITH single_route_pairs AS (
  SELECT
    r.route_id AS parent_route_id,
    MIN(rwl.location_id) FILTER (WHERE rwl.type = 'FOREIGN') AS foreign_location_id,
    MIN(rwl.location_id) FILTER (WHERE rwl.type = 'DOMESTIC') AS domestic_location_id
  FROM "route" r
  JOIN "route_warehouse_locations" rwl
    ON rwl.route_id = r.route_id
  JOIN "warehouse_location" wl
    ON wl.location_id = rwl.location_id
   AND wl.is_active = true
  WHERE r.parent_route_id IS NULL
  GROUP BY r.route_id
  HAVING COUNT(*) FILTER (WHERE rwl.type = 'FOREIGN') = 1
     AND COUNT(*) FILTER (WHERE rwl.type = 'DOMESTIC') = 1
),
created_children AS (
  INSERT INTO "route" (
    name,
    note,
    is_update_auto,
    parent_route_id
  )
  SELECT
    LEFT(
      COALESCE(parent.name, 'Route') || ' / ' ||
        COALESCE(foreign_location.name, foreign_location.location_id::text) ||
        ' -> ' ||
        COALESCE(domestic_location.name, domestic_location.location_id::text),
      255
    ),
    'Auto-created operational route from single warehouse pair',
    false,
    pairs.parent_route_id
  FROM single_route_pairs pairs
  JOIN "route" parent
    ON parent.route_id = pairs.parent_route_id
  JOIN "warehouse_location" foreign_location
    ON foreign_location.location_id = pairs.foreign_location_id
  JOIN "warehouse_location" domestic_location
    ON domestic_location.location_id = pairs.domestic_location_id
  WHERE NOT EXISTS (
    SELECT 1
    FROM "route" child
    WHERE child.parent_route_id = pairs.parent_route_id
  )
  RETURNING route_id, parent_route_id
)
INSERT INTO "route_warehouse_locations" (route_id, location_id, type)
SELECT child.route_id, pairs.foreign_location_id, 'FOREIGN'::"WarehouseType"
FROM created_children child
JOIN single_route_pairs pairs
  ON pairs.parent_route_id = child.parent_route_id
UNION ALL
SELECT child.route_id, pairs.domestic_location_id, 'DOMESTIC'::"WarehouseType"
FROM created_children child
JOIN single_route_pairs pairs
  ON pairs.parent_route_id = child.parent_route_id;

WITH single_route_pairs AS (
  SELECT
    r.route_id AS parent_route_id,
    MIN(rwl.location_id) FILTER (WHERE rwl.type = 'FOREIGN') AS foreign_location_id,
    MIN(rwl.location_id) FILTER (WHERE rwl.type = 'DOMESTIC') AS domestic_location_id
  FROM "route" r
  JOIN "route_warehouse_locations" rwl
    ON rwl.route_id = r.route_id
  JOIN "warehouse_location" wl
    ON wl.location_id = rwl.location_id
   AND wl.is_active = true
  WHERE r.parent_route_id IS NULL
  GROUP BY r.route_id
  HAVING COUNT(*) FILTER (WHERE rwl.type = 'FOREIGN') = 1
     AND COUNT(*) FILTER (WHERE rwl.type = 'DOMESTIC') = 1
),
operational_routes AS (
  SELECT
    child.route_id AS operational_route_id,
    child.parent_route_id,
    pairs.foreign_location_id,
    pairs.domestic_location_id
  FROM "route" child
  JOIN single_route_pairs pairs
    ON pairs.parent_route_id = child.parent_route_id
  WHERE child.parent_route_id IS NOT NULL
    AND EXISTS (
      SELECT 1
      FROM "route_warehouse_locations" rwl
      WHERE rwl.route_id = child.route_id
        AND rwl.location_id = pairs.foreign_location_id
        AND rwl.type = 'FOREIGN'
    )
    AND EXISTS (
      SELECT 1
      FROM "route_warehouse_locations" rwl
      WHERE rwl.route_id = child.route_id
        AND rwl.location_id = pairs.domestic_location_id
        AND rwl.type = 'DOMESTIC'
    )
)
UPDATE "orders" o
SET operational_route_id = operational.operational_route_id
FROM operational_routes operational
WHERE o.route_id = operational.parent_route_id
  AND o.operational_route_id IS NULL
  AND (
    (
      o.foreign_warehouse_location_id IS NULL
      AND o.domestic_warehouse_location_id IS NULL
    )
    OR (
      o.foreign_warehouse_location_id = operational.foreign_location_id
      AND o.domestic_warehouse_location_id = operational.domestic_location_id
    )
  );
