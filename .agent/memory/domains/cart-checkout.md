# Cart and Checkout (`Mango.Web`)

- `CartController.Checkout [POST]` validates `PickupDateTime > DateTime.Now` **server-side** before
  calling the API. The client-side `min` attribute is not trusted.
- On validation failure it reloads a fresh cart from the API, preserves the user-entered
  `PickupDateTime`, and returns `View(freshCart)` — so the user does not lose their input.
- The coupon discount is recalculated in `LoadCartDtoBasedOnLoggedInUser()`. It is **not** taken
  from the cart header sent by the client.

The `datetime-local` binding trap that affects this page is documented in
`.agent/memory/domains/ui-web.md`.
