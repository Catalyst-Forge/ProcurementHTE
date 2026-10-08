---
name: Docutrax
description: Procurement document tracking for PT Pertamina Drilling Contractor, from PR to PO without a break.
colors:
  navy-900: "#0A1A3F"
  navy-800: "#0F2A66"
  blue-600: "#2563EB"
  blue-700: "#1D4ED8"
  teal-500: "#14B8A6"
  ink: "#0F1B33"
  ink-muted: "#4A5872"
  placeholder: "#5A6880"
  line: "#D3DCE9"
  line-strong: "#B7C4D8"
  ground: "#EDF2F8"
  field: "#F6F8FC"
  card: "#FFFFFF"
  on-stage: "#FFFFFF"
  doc-fold: "#C9D6EA"
  danger: "#B42318"
  danger-ground: "#FEF0EE"
  danger-line: "#F5C2BC"
typography:
  display:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "clamp(28px, 2.6vw, 44px)"
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: "-0.02em"
  headline:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "24px"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.01em"
  body:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "13.5px"
    fontWeight: 600
  label-action:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "15.5px"
    fontWeight: 650
    letterSpacing: "0.01em"
  numeral:
    fontFamily: "Montserrat, Segoe UI, system-ui, -apple-system, Helvetica Neue, Arial, sans-serif"
    fontSize: "17px"
    fontWeight: 700
    letterSpacing: "0.04em"
    fontFeature: "tnum"
rounded:
  control: "8px"
  field: "10px"
  card: "16px"
  pill: "999px"
spacing:
  field-stack: "7px"
  inset-field: "14px"
  form-stack: "18px"
  card-inset: "40px"
components:
  button-primary:
    backgroundColor: "{colors.blue-600}"
    textColor: "{colors.card}"
    typography: "{typography.label-action}"
    rounded: "{rounded.field}"
    height: "50px"
  button-primary-hover:
    backgroundColor: "{colors.blue-700}"
  button-icon:
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink-muted}"
    rounded: "{rounded.field}"
    size: "48px"
  input-field:
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.field}"
    height: "48px"
    padding: "0 14px 0 42px"
  input-field-focus:
    backgroundColor: "{colors.card}"
  input-field-error:
    backgroundColor: "{colors.danger-ground}"
    textColor: "{colors.ink}"
  card-login:
    backgroundColor: "{colors.card}"
    rounded: "{rounded.card}"
    padding: "40px 40px 36px"
    width: "440px"
  alert-error:
    backgroundColor: "{colors.danger-ground}"
    textColor: "{colors.danger}"
    rounded: "{rounded.field}"
    padding: "12px 14px"
  stage-pill:
    backgroundColor: "{colors.on-stage}"
    textColor: "{colors.navy-900}"
    rounded: "{rounded.pill}"
    padding: "6px 14px"
---

# Design System: Docutrax

<!-- Derived from one shipped surface: the Docutrax login (Views/Auth/Login.cshtml, _LoginLayout.cshtml, wwwroot/css/login.css). The rest of the app has NOT adopted this world. -->

## Overview

**Creative North Star: "The Unbroken Track"**

Docutrax is drawn as a track that never breaks. The brand mark, a blue-to-teal infinity, becomes the stage: on a deep navy field with a faint dot grid, the mark is drawn at large scale as a road, and small white document tokens travel along it past the procurement stages. Next to it, the working side is quiet and light: a pale blue-grey ground, one white card, one blue action. The world splits cleanly. Navy holds the brand, light holds the task, and the two never mix.

The density is that of a daily tool, not a marketing page. One family (Montserrat) carries the whole hierarchy through weight alone. Shapes are softly rounded (10px fields, 16px card, full pills). Depth is soft, diffuse lift and never a hard offset. Motion has two registers: a single entrance (the track draws itself, the stage pills rise in sequence) and one continuous, slow ambient loop (three documents moving along the track). Both stop completely under reduced motion.

**Scope and status.** This system is derived from a single shipped surface, the login page. The dashboard, the procurement screens, and the other auth pages that use `_AuthenticationLayout` (including Forgot Password, which the login links to) still use the older Bootstrap look and have **not adopted** this world yet. Tokens live only in `login.css` as `--dx-*`. Nothing here has been checked against a data-dense screen.

**Open decision: the left-panel world is not confirmed.** The client pinned the split composition (visual panel on the left, white login card on the right) and the form contents. The left-panel world, the logo-as-track illustration, was the one free axis. It was not rolled and the user has not yet confirmed it. Treat the Track Stage component and everything derived from it (navy stage, dot grid, document tokens, stage pills) as provisional until it is confirmed. The light working side, the palette roles, the type, and the form components rest on the brand mark and the client's pins, so they are firmer.

**Key Characteristics:**
- Two grounds: a deep navy brand stage and a pale blue-grey task ground.
- The infinity mark as the only source of the blue-to-teal gradient.
- One action colour (blue), used sparingly.
- One typeface, Montserrat, self-hosted, with hierarchy carried by weight.
- Soft, diffuse elevation. No hard shadows.
- A one-time entrance plus one slow ambient loop, with everything paused under reduced motion.

## Colors

A cool, saturated navy-and-blue palette with one teal that belongs to the brand mark, set against near-white blue-grey neutrals.

### Primary
- **Docutrax Blue** (blue-600): the only action colour. Used for the primary button, link text, focus borders and outlines, the caret, checkbox accent, and icon-button hover tint. It is also the first stop of the brand gradient and the "active line" on the document token.
- **Pressed Blue** (blue-700): the hover and busy state of the primary button, and the hover state of links. It is never used as a resting colour.

### Secondary
- **Track Teal** (teal-500): the end stop of the brand gradient (blue-600 → #0EA5D6 → teal-500) on the infinity track and in the logo. It also tints text selection on the navy stage. It never fills a control.

### Tertiary
- **Stage Navy** (navy-900): the deep end of the left-panel field, the fill inside the track's stage nodes, stage-pill text, and the browser `theme-color`.
- **Stage Navy Light** (navy-800): the light end of the stage gradient (160deg, navy-900 → navy-800). On the light side it sets the captcha numerals.

### Neutral
- **Ink** (ink): all primary text on the light side.
- **Muted Ink** (ink-muted): the lede, help text, field icons, and resting icon buttons.
- **Placeholder Slate** (placeholder): placeholder text and loading text. It is darker than a typical placeholder grey so it stays legible on the field fill.
- **Hairline** (line): resting borders of fields and icon buttons.
- **Firm Hairline** (line-strong): field hover border and the dashed border of the captcha question. Currently hard-coded in CSS, not a `--dx-*` variable.
- **Blue-Grey Ground** (ground): the page background on the task side.
- **Field Wash** (field): resting fill of inputs and icon buttons. Inputs turn white (card) on focus.
- **Card White** (card): the login card and the focused input.
- **On-Stage White** (on-stage): all text on the navy stage, the node rings, and stage pills.
- **Fold Grey** (doc-fold): the folded corner and secondary lines of the document token (SVG literal).

### Feedback
- **Danger** (danger), **Danger Wash** (danger-ground), **Danger Line** (danger-line): the error alert, invalid-field border and fill, and field error text. Error focus uses a red ring (`rgba(180, 35, 24, 0.16)`).

### Named Rules
**The One Blue Rule.** Blue-600 is the only colour that means "act here". Hover deepens it to blue-700. No other hue fills a button or marks focus.

**The Gradient Belongs to the Mark Rule.** The blue-to-teal gradient appears only on the infinity: the logo and the track drawn from it. Never apply it to buttons, headings, borders, or backgrounds.

**The Two Grounds Rule.** Navy is the brand's stage. The task always sits on the light ground inside a white card. Form controls never sit on navy.

## Typography

**Display Font:** Montserrat (variable, weights 100–900, self-hosted from `wwwroot/lib/fonts/montserrat/` under the OFL), with a fallback to Segoe UI, system-ui, and Arial.
**Body Font:** Montserrat, with the same fallback.

**Character:** A geometric sans with wide, open capitals that echo the spaced DOCUTRAX wordmark. One family keeps the page calm. Weight does all the hierarchical work.

### Hierarchy
- **Display** (700, clamp(28px, 2.6vw, 44px), 1.15, -0.02em, balanced wrap): the single statement on the navy stage. White. Hidden below 992px.
- **Headline** (700, 24px, 1.25, -0.01em; 22px under 480px): the card title, centred.
- **Body** (400, 15px, 1.5): the lede, input text, and checkbox labels (14px). Help text is 13.5px in muted ink.
- **Label** (600, 13.5px): field labels and inline links. The stage pills use the same weight at clamp(11px, 0.95vw, 13.5px) with +0.01em tracking.
- **Label Action** (650, 15.5px, +0.01em): the primary button. It uses an intermediate weight that the variable font allows.
- **Numeral** (700, 17px, +0.04em, tabular figures): the captcha question. Tabular figures also apply to the captcha answer input.

### Named Rules
**The One Family Rule.** Montserrat is the only face. Do not add a second display or mono family. Reach for weight (400 / 600 / 650 / 700) before size.

## Layout

On the login page the canvas is a two-column grid, `minmax(0, 1.4fr) minmax(440px, 1fr)`, so the stage takes about 58% at desktop width. Both columns fill the viewport height (`100dvh`). Below 1200px the ratio relaxes to `1.15fr / minmax(420px, 1fr)`. Below 992px it collapses to a single column. The stage becomes a short band above the card, holding only the track (max 280px wide). The pills and the display line hide. The card overlaps the band by 12px. In the card, the full logo is swapped for the wordmark, because the band already shows the infinity mark. Below 480px the card inset tightens to 28px 20px 24px.

The stage centres its content vertically, with fluid padding (clamp(40px, 6vw, 88px)) and a fluid gap (clamp(32px, 6vh, 64px)). The track figure is capped at 760px. The display line is inset 5.36% so it aligns with the drawn edge of the track, not the SVG viewBox.

The task column centres a card capped at 440px. Inside it, spacing is set per element rather than on a strict scale. Fields stack at 18px, with 7px between a label and its control. Insets are 14px. The card padding is 40px. The help line sits 20px below the card.

## Elevation & Depth

The system uses soft, diffuse lift with negative spread, tinted toward ink or blue. There are no hard offsets and no outlines-as-shadow. The navy stage gets its depth from a gradient and a dot grid (1px dots at 16% on-stage-muted tone, 26px pitch), not from shadow.

### Shadow Vocabulary
- **Card lift** (`0 2px 6px rgba(15,27,51,0.05), 0 24px 48px -24px rgba(15,27,51,0.28)`): the login card, together with a 6% ink hairline border.
- **Action glow** (`0 10px 20px -10px rgba(37,99,235,0.7)`; hover `0 12px 24px -10px rgba(29,78,216,0.75)`): the primary button only. A blue-tinted pool underneath, not a drop shadow.
- **Pill lift** (`0 8px 18px -10px rgba(0,0,0,0.6)`): stage pills on navy.
- **Token shadow** (`drop-shadow(0 6px 8px rgba(4,12,32,0.45))`): document tokens on the track.
- **Focus ring** (`0 0 0 3px rgba(37,99,235,0.18)`, with the border shifting to blue-600): focused inputs. Buttons, links, and checkboxes use a 2px blue-600 outline with a 2px offset instead.

### Named Rules
**The Soft Lift Rule.** Every shadow has a negative spread or a large blur and a tint drawn from the palette. If a shadow reads as an offset copy of the shape, it is wrong.

## Shapes

Corners are gently rounded and consistent by role. Fields, alerts, the captcha tiles, and the primary button share a 10px radius. The card is 16px. Icon-button hover plates are 8px. Stage pills are fully round (999px). The document token is a small rounded sheet with a folded top-right corner. The infinity track is a thick stroke (80 units on a 1000-unit drawing) of smooth Bezier loops, with butt ends where it breaks at the centre crossing. Borders are 1px hairlines. The captcha question is the one dashed border, which marks it as a generated, non-editable value.

## Components

### Buttons
Firm and plain: one solid blue bar, no outline variant.
- **Shape:** gently rounded (10px), 50px tall, full card width.
- **Primary:** blue-600 fill, white Label Action text, action glow.
- **Hover / Focus:** fill deepens to blue-700 with a stronger glow. Focus-visible shows a 2px blue-600 outline at 2px offset. Active presses down 1px.
- **Busy:** on submit, the label hides and a 20px white ring spinner (0.7s) takes its place. The button is disabled with `cursor: progress`. It resets on bfcache restore.
- **Icon button:** a 48px square (captcha refresh) or a 36px button inside a field (password toggle). Muted ink at rest. On hover it gets an 8% blue plate and blue-600 icon. The refresh icon spins while a new question loads.

### Inputs / Fields
- **Style:** 48px tall, field-wash fill, 1px hairline border, 10px radius, a leading 17px icon in muted ink at 14px inset, and 42px text indent.
- **Hover:** border firms to line-strong.
- **Focus:** fill turns white, border turns blue-600, and a 3px blue ring at 18% appears. No default outline.
- **Error:** border turns danger, fill turns danger wash, and the focus ring turns red. The message appears below in 13px/500 danger text, and empty messages collapse.
- **Label row:** the label (600, 13.5px) can share its row with a right-aligned blue link ("Lupa password?"), which underlines on hover.
- **Checkbox:** native 18px box with a blue-600 accent colour. The label is 14px ink.

### Captcha Row
A three-part row: question tile, answer field, refresh button (`1fr 1fr 48px`, 8px gap). The question tile is a dashed-border, pale-blue (#E9F0FB) tile with navy-800 tabular numerals. While loading it shows placeholder slate. The answer input is centred, numeric-only, and has no leading icon.

### Cards / Containers
- **Corner Style:** 16px.
- **Background:** card white on the blue-grey ground.
- **Shadow Strategy:** card lift (see Elevation), plus a 6% ink hairline.
- **Internal Padding:** 40px 40px 36px, and 28px 20px 24px under 480px.
- **Content order:** logo (112px on desktop; wordmark 168px or 152px on narrow screens), headline, muted lede, optional alert, form.

### Alert
The form-level error is a 10px-radius danger-wash box with a danger-line border, 14px/500 danger text, and a leading 16px icon. It carries `role="alert"`.

### Track Stage (signature component; provisional, see Overview)
The infinity mark redrawn as a road on the navy stage. The track is an SVG path stroked with the brand gradient (blue-600 → #0EA5D6 → teal-500). It is broken at the centre crossing so the overpass reads the way the logo does. Four node rings (navy fill, white 5-unit stroke) sit at the top and bottom of each loop. White stage pills anchor to them: on the left loop, Purchase Requisition above and Approval berjenjang below; on the right loop, Vendor above and Purchase Order below. Three document tokens (white sheet, folded corner, one blue line and two fold-grey lines) travel the path via SMIL `animateMotion`. The route runs centre → left loop → right loop over 16s, and the tokens are phased a third apart.

- **Entrance:** the track draws in over 1.8s (`cubic-bezier(0.16, 1, 0.3, 1)`). Pills rise 8px and fade in over 0.9s, staggered 150ms from 0.5s.
- **Reduced motion:** CSS stops the draw and the rise. JavaScript calls `pauseAnimations()` on the SVG, because SMIL ignores CSS media queries. Every new SMIL motion must be wired the same way.
- **Narrow screens:** the track only, no pills, no display line.
- The whole figure is `aria-hidden`. It carries no information that the form needs.

## Do's and Don'ts

### Do:
- **Do** keep blue-600 as the single action colour, with blue-700 for hover and busy states.
- **Do** keep the blue-to-teal gradient on the infinity mark and the track drawn from it, and nowhere else.
- **Do** put tasks on the blue-grey ground inside white cards with 16px corners and the card lift.
- **Do** use Montserrat for everything, and build hierarchy with weight (400 / 600 / 650 / 700).
- **Do** give every field the 48px height, 10px radius, field-wash fill, and the blue border-plus-ring focus.
- **Do** pair every animation with a reduced-motion stop, and pause SMIL explicitly with `pauseAnimations()`.
- **Do** use tabular figures wherever numbers are compared or typed (the captcha).

### Don't:
- **Don't** fill buttons, focus rings, or states with teal. Teal lives in the brand gradient.
- **Don't** place form controls or body copy on the navy stage.
- **Don't** use hard, offset, or untinted heavy shadows. Lift is soft and diffuse.
- **Don't** add a second typeface or fall back to a system-only display stack.
- **Don't** add the Pertamina PDC logo to Docutrax surfaces until the client confirms it.
- **Don't** invent taglines, statistics, or claims on the stage. Docutrax has no official tagline or illustration library yet.
- **Don't** treat this world as already adopted elsewhere. Pages that still use `_AuthenticationLayout` and the Bootstrap app shell are the old look, not drift from this system.
