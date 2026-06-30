/*
  Warnings:

  - A unique constraint covering the columns `[route_code]` on the table `route` will be added. If there are existing duplicate values, this will fail.

*/
-- AlterTable
ALTER TABLE "route" ADD COLUMN     "route_code" VARCHAR(50);

-- CreateIndex
CREATE UNIQUE INDEX "route_route_code_key" ON "route"("route_code");
