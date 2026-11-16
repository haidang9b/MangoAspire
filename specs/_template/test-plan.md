# Test plan

## Coverage strategy
<Which layers cover which acceptance criteria? Backend logic → xUnit + Moq + Shouldly in `tests/`.
The SPA has no test runner, so its ACs map to Lint/Build (`pnpm --dir src/UI/mango-ui lint` and
`build`) or Manual.>

| AC    | Unit (xUnit) | Integration | Lint/Build | Manual |
|-------|--------------|-------------|------------|--------|
| AC-1  |      x       |             |            |        |
| AC-2  |              |      x      |            |        |

## Scenarios
- Priority 1 (main flow / required fields): <cases>
- Priority 2 (functional / validation): <cases>
- Priority 3 (unhappy / edge / permission): <cases>
- Priority 4 (UI / rare): <cases>

## Risks
<Areas under-covered, flaky dependencies, data setup pain points.>
