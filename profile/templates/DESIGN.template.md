# DESIGN.md

## Purpose

Define **repeatable UI element designs and layouts** for the project.

- `THEME.md` handles **colors, fonts, and theme-wide styling**.
- `DESIGN.md` captures **structural, reusable component patterns** and page/layout conventions.

## Design Principles

- UX priority rules (clarity over novelty, calm over clutter)
- Accessibility expectations (keyboard navigation, contrast, font sizing)
- Faith/value tone expectations (prayerful, non-extractive, consent-forward)

## Component Design Catalog

Capture one section per reusable component pattern.

### Component: <Name>

- **Intent:** What problem this component solves
- **Where used:** Pages/flows/modules
- **States:** loading / empty / error / disabled / success
- **Inputs:** (props) and required parameters
- **Outputs:** emitted events or form submission behavior
- **Validation:** client-side validation rules (and server-side expectations if relevant)
- **Accessibility:** labels, aria attributes, focus handling, keyboard behavior
- **Styling hooks:** classNames / design tokens referenced from `THEME.md`

#### Layout & Composition

- Recommended layout structure (grid/flex)
- Example markup skeleton (pseudo-code is fine)

#### Example Usage

- Short example of how a page/feature would use this component

---

## Form Patterns

### Input (Text)

- Label + helper text rules
- Character limits and error messaging
- Optional/required indicators

### Dropdown / Select

- Search vs non-search behavior
- Default/placeholder behavior
- Multi-select vs single-select

### Buttons and Actions

- Primary vs secondary vs destructive
- Loading/spinner behavior
- Disabled rules

---

## AI Assistant Widget Patterns

These patterns describe how AI outputs are presented, confirmed, and persisted.

### Widget: Chat Assistant (general)

- **User goals:** what the widget is for
- **Message types:** user / assistant / system / tool-result (if applicable)
- **AI safety posture:** how the UI communicates uncertainty or refusal
- **Consent boundaries:** what requires explicit user confirmation before saving

#### Conversation-to-Structured-Data

- How chat outputs are transformed into structured data (e.g., gratitude items)
- Review UI: what the user sees before writing
- “Edit before save” workflow

#### Streaming vs non-streaming

- If you stream tokens, how you handle partial messages
- Fallback behavior if streaming isn’t available

---

## Page/Layout Conventions

- Landing page sections and ordering rules
- Auth pages (login/register) layout
- Primary app dashboard layout (grids/spacing)

## Navigation & Routing Conventions

- Top nav vs side nav rules
- Breadcrumbs or page titles conventions

## Component Interactions

- Modals: when used, how dismissed, focus trap rules
- Toasts/alerts: when shown and how they behave
- Empty states: messaging patterns

## Notes / Decisions Log

- Record important design decisions and why they were made
