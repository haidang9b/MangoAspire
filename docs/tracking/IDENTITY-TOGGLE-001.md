# Ticket: IDENTITY-TOGGLE-001 Tracker

## Step 1: Analysis and Planning
- [x] Review `analyze-requirement` workflow and `analyze-po-requirement` skill
- [x] Analyze impact on `Mango.AppHost` (Orchestration)
- [x] Analyze impact on individual microservices (Authentication Middleware)
- [x] Analyze impact on Frontend applications (`Mango.Web` and `mango-ui`)
- [x] Create Technical Blueprint and Clarifying Questions
- [ ] Obtain user approval for the plan

## Step 2: Implementation (Core Infrastructure)
- [ ] Update `Mango.AppHost` to support conditional service registration based on a feature flag
- [ ] Create a shared configuration/extension for authentication that respects the flag

## Step 3: Implementation (Microservices)
- [ ] Update `Products.API`, `ShoppingCart.API`, `Orders.API`, etc.
- [ ] Ensure `WebApplicationBuilderExtensions` can handle both OIDC providers

## Step 4: Implementation (Frontend & Gateway)
- [ ] Update `Mango.Gateway` (YARP) routing if necessary
- [ ] Update `Mango.Web` and `mango-ui` to point to the correct authority

## Step 5: Verification
- [ ] Verify system functionality with `Identity.API` enabled
- [ ] Verify system functionality with `OpenIdentity.App` enabled
- [ ] Final documentation update and walkthrough
