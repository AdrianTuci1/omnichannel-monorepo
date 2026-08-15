import { ORDER_STATUS } from "../types.js";

export default function StatusBadge({ status }) {
  const info = ORDER_STATUS[status] ?? { label: status, tone: "neutral" };
  return <span className={`badge badge-${info.tone}`}>{info.label}</span>;
}
