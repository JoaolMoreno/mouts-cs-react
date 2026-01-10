# Fictional Company - Frontend Design System

This document outlines the visual identity and technical style guide for the Fictional Company web application.

## 🎨 Core Aesthetic
**"Corporate Premium"**
A sophisticated, professional look anchored in deep greens, ensuring a trustworthy and modern corporate presence. The design prioritizes shared global styles over component-specific overrides.

## 🔤 Typography

We utilize a single, versatile font family to maintain strict consistency across the application.

- **Primary Font**: **'Outfit'**, sans-serif.
  - A modern geometric sans-serif that feels both professional and approachable.
  - **Weights**:
    - `300` (Light): Large headlines, display text.
    - `400` (Regular): Body text, inputs.
    - `500` (Medium): Navigation, subheadings.
    - `700` (Bold): CTAs, emphasized data.

### Hierarchy Definitions (SCSS Mixins Recommended)

| Element | Size | Weight | Line Height | Tracking | Usage |
|:---|:---|:---|:---|:---|:---|
| **H1** | `3.5rem` (56px) | 300/700 | `1.1` | `-0.02em` | Main Page Titles |
| **H2** | `2.5rem` (40px) | 500 | `1.2` | `-0.01em` | Section Headers |
| **H3** | `1.75rem` (28px) | 500 | `1.3` | `0` | Card Titles |
| **Body** | `1rem` (16px) | 400 | `1.6` | `0` | Standard Content |
| **Small**| `0.875rem` (14px)| 400 | `1.5` | `0` | Metadata, Helpers |

---

## 🎨 Color Palette

### Theme Strategy: Corporate Light
The application uses a "Light Mode" workspace for readability and adding a corporate feel

### Primary Brand Colors (Deep Forest)
Used for Navigation (Sidebar), Navbar, and Brand Accents.
| Token | Hex Value | Description |
|:---|:---|:---|
| **Deep Forest** | `#051F1A` | Sidebar Background. Deepest green. |
| **Corporate Green**| `#0C3B32` | Secondary Brand Color. |
| **Vibrant Leaf** | `#10B981` | Primary Actions / Highlights. |

### Workspace Colors (Light Theme)
Used for the main content area, cards, and text.
| Token | Hex Value | Description |
|:---|:---|:---|
| **Page Background** | `#F3F4F6` | Light Gray (Sterile/Clean). |
| **Surface/Card** | `#FFFFFF` | White. |
| **Text Main** | `#111827` | Dark Gray/Black for readability on light. |
| **Text Muted** | `#6B7280` | Metadata/Labels. |

### Functional Colors
- **Text Primary**: `#F9FAFB` (Off-white) - Used on Dark Backgrounds.
- **Error**: `#EF4444` (Soft Red).
-   **Success**: `#10B981` (Vibrant Leaf - same as primary accent).
-   **Warning**: `#F59E0B`.

---

## 🛠️ SCSS Architecture

We follow a **Global-First** approach. Components should rely on global mixins and variables rather than hardcoded values.

### Directory Structure
```text
styles/
├── abstract/
│   ├── _variables.scss      # Colors, Fonts, Spacing, Z-index
│   ├── _mixins.scss         # Responsive, Typography helpers, Flexbox utils
│   └── _functions.scss      # Calculations (px to rem, etc.)
├── base/
│   ├── _reset.scss          # CSS Reset (modern-normalize)
│   ├── _typography.scss     # Global H1-H6, p, a definitions
│   └── _globals.scss        # Body background, generic utility classes
└── components/              # ONLY for truly complex, isolated styles
    └── ...
```

### Usage Rules
1.  **No Hex Codes in Components**: Always use `$variables`.
2.  **Typography via Mixins**: Use `@include text-h1;` instead of raw font-size everywhere.
3.  **Spacing Scale**: Use a geometric spacing scale (4px, 8px, 16px, 24px, 32px...) defined in variables.
4.  **Dark Mode Default**: The application is "Dark Mode" by default.

---

## 🧱 Component Styling Policy
- **Global Patterns**: Buttons, Inputs, and Cards must be defined in `base/` or a shared UI folder.
- **Minimal Overrides**: A generic card component should handle padding and background. Page-specific styles should only control layout/positioning.
