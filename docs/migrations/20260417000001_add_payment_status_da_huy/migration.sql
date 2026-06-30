-- Add DA_HUY to PaymentStatus enum (used when a pending payment is cancelled alongside an order cancel-approve)
ALTER TYPE "PaymentStatus" ADD VALUE IF NOT EXISTS 'DA_HUY';
