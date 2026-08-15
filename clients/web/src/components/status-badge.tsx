import { Badge, type BadgeProps } from "@/components/ui/badge";
import type { OrderStatus } from "@/lib/types";

const STATUS_VARIANT: Record<OrderStatus, BadgeProps["variant"]> = {
  Draft: "muted",
  Pending: "secondary",
  Paid: "default",
  Shipped: "outline",
  Delivered: "success",
  Cancelled: "outline",
};

export function StatusBadge({ status }: { status: OrderStatus }) {
  return (
    <Badge variant={STATUS_VARIANT[status] ?? "secondary"}>{status}</Badge>
  );
}
