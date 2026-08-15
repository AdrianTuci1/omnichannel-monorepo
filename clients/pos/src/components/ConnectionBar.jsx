import { useState } from "react";

export default function ConnectionBar({ baseUrl, health, onSaveUrl }) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(baseUrl);

  const online = health && health.status === "ok";

  const save = () => {
    const next = draft.trim() || baseUrl;
    onSaveUrl(next);
    setDraft(next);
    setEditing(false);
  };

  const cancel = () => {
    setDraft(baseUrl);
    setEditing(false);
  };

  return (
    <div className="connection-bar">
      <span
        className={`status-dot ${online ? "online" : "offline"}`}
        title={online ? "API conectat" : "API indisponibil"}
      />
      {editing ? (
        <>
          <input
            className="url-input"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            placeholder="http://localhost:5000"
            autoFocus
          />
          <button className="btn" onClick={save}>
            Salvează
          </button>
          <button className="btn btn-ghost" onClick={cancel}>
            Anulează
          </button>
        </>
      ) : (
        <>
          <span className="url-label">{baseUrl}</span>
          <button
            className="btn btn-ghost"
            onClick={() => {
              setDraft(baseUrl);
              setEditing(true);
            }}
          >
            Editează URL
          </button>
        </>
      )}
    </div>
  );
}
