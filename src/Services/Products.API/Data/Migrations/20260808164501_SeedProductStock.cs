using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Products.API.Data.Migrations
{
    /// <summary>
    /// Gives the seeded catalogue a non-zero stock level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every row inserted by <c>AddSeedData</c> carries <c>available_stock = 0</c>, and nothing in
    /// the seed path ever sets it. That was invisible while the column was excluded from CDC; now
    /// that ChatAgent replicates it, shipping as-is would have the agent truthfully report the
    /// entire menu as unavailable.
    /// </para>
    /// <para>
    /// It also fixes a pre-existing defect this merely exposed: <c>ReserveProductStock</c> fails
    /// the checkout saga for any product with insufficient stock, which today is all of them.
    /// </para>
    /// </remarks>
    public partial class SeedProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Predicated on zero so this is idempotent, and so it leaves alone any environment
            // where an operator has already set real stock levels.
            migrationBuilder.Sql("UPDATE products SET available_stock = 100 WHERE available_stock = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty. Restoring zeros would take stock away from live carts to undo a
            // data fix, which is worse than leaving the corrected values in place.
        }
    }
}
