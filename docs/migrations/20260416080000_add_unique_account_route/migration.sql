-- AddUniqueConstraint
CREATE UNIQUE INDEX "account_route_account_id_route_id_key" ON "account_route"("account_id", "route_id");
