/*
  Warnings:

  - You are about to drop the column `assigned_route_id` on the `staff` table. All the data in the column will be lost.

*/
-- DropForeignKey
ALTER TABLE "staff" DROP CONSTRAINT "staff_assigned_route_id_fkey";

-- AlterTable
ALTER TABLE "staff" DROP COLUMN "assigned_route_id";
