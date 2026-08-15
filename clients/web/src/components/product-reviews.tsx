"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";

import { apiGet, apiPost } from "@/lib/api";
import {
  formatDate,
  type CreateReviewRequest,
  type Review,
} from "@/lib/types";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

const inputClass =
  "w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-1 focus:ring-neutral-400";
const labelClass = "text-sm font-medium text-neutral-700";

function Stars({ rating }: { rating: number }) {
  return (
    <span
      className="text-sm font-medium text-neutral-900"
      aria-label={`${rating} din 5`}
    >
      {"★".repeat(rating)}
      <span className="text-neutral-300">{"★".repeat(5 - rating)}</span>
    </span>
  );
}

export function ProductReviews({ productId }: { productId: string }) {
  const [reviews, setReviews] = useState<Review[] | null>(null);
  const [rating, setRating] = useState("5");
  const [title, setTitle] = useState("");
  const [comment, setComment] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadReviews = useCallback(() => {
    setReviews(null);
    apiGet<Review[]>(`/products/${productId}/reviews`)
      .then(setReviews)
      .catch(() => setReviews([]));
  }, [productId]);

  useEffect(() => {
    loadReviews();
  }, [loadReviews]);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    const payload: CreateReviewRequest = {
      rating: Number(rating),
      title: title.trim(),
      comment: comment.trim(),
      customerId: customerId.trim(),
    };

    try {
      const created = await apiPost<Review>(
        `/products/${productId}/reviews`,
        payload
      );
      setTitle("");
      setComment("");
      setCustomerId("");
      setRating("5");
      setReviews((prev) => [created, ...(prev ?? [])]);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Eroare la trimiterea recenziei."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Recenzii</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-6">
        {reviews === null ? (
          <p className="text-sm text-neutral-500">Se încarcă recenziile…</p>
        ) : reviews.length === 0 ? (
          <p className="text-sm text-neutral-500">
            Nu există încă recenzii. Fii primul care lasă una.
          </p>
        ) : (
          <div className="flex flex-col gap-3">
            {reviews.map((review) => (
              <div
                key={review.id}
                className="rounded-md border border-neutral-200 px-3 py-2"
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm font-medium text-neutral-900">
                    {review.title}
                  </span>
                  <Stars rating={review.rating} />
                </div>
                <p className="mt-1 text-sm text-neutral-700">{review.comment}</p>
                <div className="mt-2 flex items-center gap-2 text-xs text-neutral-500">
                  <span>Client: {review.customerId}</span>
                  <span>·</span>
                  <span>{formatDate(review.createdAt)}</span>
                </div>
              </div>
            ))}
          </div>
        )}

        <form
          onSubmit={handleSubmit}
          className="flex flex-col gap-3 rounded-md border border-neutral-200 p-4"
        >
          <p className="text-sm font-semibold text-neutral-900">
            Adaugă o recenzie
          </p>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="flex flex-col gap-1">
              <label htmlFor={`rating-${productId}`} className={labelClass}>
                Rating
              </label>
              <select
                id={`rating-${productId}`}
                className={inputClass}
                value={rating}
                onChange={(e) => setRating(e.target.value)}
              >
                {[1, 2, 3, 4, 5].map((value) => (
                  <option key={value} value={value}>
                    {value} — {value === 1 ? "1 stea" : `${value} stele`}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1">
              <label htmlFor={`title-${productId}`} className={labelClass}>
                Titlu
              </label>
              <input
                id={`title-${productId}`}
                className={inputClass}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                placeholder="Rezumat"
              />
            </div>
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor={`comment-${productId}`} className={labelClass}>
              Comentariu
            </label>
            <textarea
              id={`comment-${productId}`}
              className={inputClass}
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              rows={3}
              required
              placeholder="Părerea ta despre produs"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor={`customerId-${productId}`} className={labelClass}>
              ID client (GUID)
            </label>
            <input
              id={`customerId-${productId}`}
              className={inputClass}
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              required
              placeholder="GUID client"
            />
          </div>

          {error ? (
            <p className="rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900">
              {error}
            </p>
          ) : null}

          <div>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Se trimite…" : "Trimite recenzia"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
