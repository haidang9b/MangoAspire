# TICKET-ID — Technical Blueprint

*Produced by the `analyze-requirement` workflow, approved by the user before any code is written.
Progress against this plan is tracked in `ticket.json`, not here.*

## Requirement
*The PO requirement in one paragraph, plus the acceptance criteria as a plain list.*

## Backend
- **Slice:** which service and `Features/<FeatureName>/` folder
- **Command/Query + Handler:** names and the `ResultModel<T>` payload
- **Validator:** the FluentValidation rules
- **Route:** the Minimal API endpoint in `Routes/` (kebab-case, REST noun)
- **Persistence:** `DbContext` configuration and whether an EF Core migration is required
- **Integration events:** published or handled, if any

## Frontend
- **Components:** new or changed components under `src/UI/mango-ui/src/`
- **API client:** the call added to `src/UI/mango-ui/src/api/`
- **State:** which existing context is reused (`AuthContext`, cart, theme, notification)
- **Types:** the TypeScript interfaces that must match the backend DTOs

## Touchpoints
*Exact files this ticket will modify, from the context search.*

## Risks and open questions
*What could break, and what needs a decision from the user.*
