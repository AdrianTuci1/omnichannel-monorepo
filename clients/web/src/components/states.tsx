import { AlertCircle } from "lucide-react";

import { Spinner } from "@/components/ui/spinner";

export function LoadingState({ label = "Se încarcă datele…" }: { label?: string }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border border-neutral-200 bg-white p-6 text-neutral-600">
      <Spinner />
      <span className="text-sm">{label}</span>
    </div>
  );
}

export function ErrorState({ message }: { message: string }) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-neutral-300 bg-white p-6">
      <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-neutral-700" />
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium text-neutral-900">
          Nu am putut încărca datele
        </span>
        <span className="text-sm text-neutral-600">{message}</span>
        <span className="text-xs text-neutral-500">
          Verifică dacă StoreApi rulează la adresa configurată
          (NEXT_PUBLIC_API_BASE_URL).
        </span>
      </div>
    </div>
  );
}

export function EmptyState({ label }: { label: string }) {
  return (
    <div className="rounded-lg border border-dashed border-neutral-300 bg-white p-8 text-center text-sm text-neutral-500">
      {label}
    </div>
  );
}
