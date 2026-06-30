-- Each operational child route represents one FOREIGN -> DOMESTIC warehouse pair
-- inside its parent route. Existing duplicates must be cleaned separately before
-- those rows are edited; the trigger prevents new duplicate pair writes.
CREATE OR REPLACE FUNCTION public.prevent_duplicate_operational_route_pair()
RETURNS TRIGGER AS $$
DECLARE
  v_parent_route_id BIGINT;
  v_foreign_location_id BIGINT;
  v_domestic_location_id BIGINT;
BEGIN
  SELECT r.parent_route_id
  INTO v_parent_route_id
  FROM "route" r
  WHERE r.route_id = NEW.route_id;

  -- Parent routes may keep many warehouse locations. The invariant applies only
  -- after an operational child has both sides of its warehouse pair.
  IF v_parent_route_id IS NULL THEN
    RETURN NEW;
  END IF;

  IF NEW.type = 'FOREIGN'::"WarehouseType" THEN
    v_foreign_location_id := NEW.location_id;
    SELECT rwl.location_id
    INTO v_domestic_location_id
    FROM "route_warehouse_locations" rwl
    WHERE rwl.route_id = NEW.route_id
      AND rwl.type = 'DOMESTIC'::"WarehouseType"
      AND (
        TG_OP <> 'UPDATE' OR
        (rwl.location_id, rwl.type) IS DISTINCT FROM (OLD.location_id, OLD.type)
      )
    LIMIT 1;
  ELSE
    v_domestic_location_id := NEW.location_id;
    SELECT rwl.location_id
    INTO v_foreign_location_id
    FROM "route_warehouse_locations" rwl
    WHERE rwl.route_id = NEW.route_id
      AND rwl.type = 'FOREIGN'::"WarehouseType"
      AND (
        TG_OP <> 'UPDATE' OR
        (rwl.location_id, rwl.type) IS DISTINCT FROM (OLD.location_id, OLD.type)
      )
    LIMIT 1;
  END IF;

  IF v_foreign_location_id IS NULL OR v_domestic_location_id IS NULL THEN
    RETURN NEW;
  END IF;

  -- Serialize concurrent writes for the same logical operational route pair.
  PERFORM pg_advisory_xact_lock(
    hashtextextended(
      v_parent_route_id::TEXT || ':' || v_foreign_location_id::TEXT || ':' || v_domestic_location_id::TEXT,
      0
    )
  );

  IF EXISTS (
    SELECT 1
    FROM "route" child
    JOIN "route_warehouse_locations" child_foreign
      ON child_foreign.route_id = child.route_id
     AND child_foreign.type = 'FOREIGN'::"WarehouseType"
     AND child_foreign.location_id = v_foreign_location_id
    JOIN "route_warehouse_locations" child_domestic
      ON child_domestic.route_id = child.route_id
     AND child_domestic.type = 'DOMESTIC'::"WarehouseType"
     AND child_domestic.location_id = v_domestic_location_id
    WHERE child.parent_route_id = v_parent_route_id
      AND child.route_id <> NEW.route_id
  ) THEN
    RAISE EXCEPTION
      USING ERRCODE = '23505',
        MESSAGE = 'Operational route warehouse pair already exists for parent route';
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_prevent_duplicate_operational_route_pair_ins
  ON "route_warehouse_locations";

DROP TRIGGER IF EXISTS trg_prevent_duplicate_operational_route_pair_upd
  ON "route_warehouse_locations";

CREATE TRIGGER trg_prevent_duplicate_operational_route_pair_ins
BEFORE INSERT
ON "route_warehouse_locations"
FOR EACH ROW
EXECUTE FUNCTION public.prevent_duplicate_operational_route_pair();

CREATE TRIGGER trg_prevent_duplicate_operational_route_pair_upd
BEFORE UPDATE OF location_id, type
ON "route_warehouse_locations"
FOR EACH ROW
WHEN (
  NEW.location_id IS DISTINCT FROM OLD.location_id OR
  NEW.type        IS DISTINCT FROM OLD.type
)
EXECUTE FUNCTION public.prevent_duplicate_operational_route_pair();
