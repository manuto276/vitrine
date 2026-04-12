import React from 'react';

export default function AboutPage() {
  return (
    <div>
      <h1 className="page-title">About</h1>

      <div className="card" style={{ marginBottom: 16 }}>
        <div className="about-logo">Vitrine</div>
        <div className="about-version">Desktop widget engine for Windows</div>
        <div className="about-links">
          <a href="https://github.com" target="_blank" rel="noreferrer">
            GitHub Repository
          </a>
        </div>
      </div>

      <div className="card-group-title">Info</div>

      <div className="card">
        <div className="card-row">
          <span className="card-label">Runtime</span>
          <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>.NET 9 + WebView2</span>
        </div>
      </div>
      <div className="card">
        <div className="card-row">
          <span className="card-label">Theme Engine</span>
          <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>React 18 (IIFE)</span>
        </div>
      </div>
      <div className="card">
        <div className="card-row">
          <span className="card-label">License</span>
          <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>MIT</span>
        </div>
      </div>
    </div>
  );
}
