import { cn } from "@/lib/utils";

export function Spinner({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "inline-block h-5 w-5 animate-spin rounded-full border-2 border-neutral-300 border-t-neutral-900",
        className
      )}
      role="status"
      aria-label="Se încarcă"
    />
  );
}
