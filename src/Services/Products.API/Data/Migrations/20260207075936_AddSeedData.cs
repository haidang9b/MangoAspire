using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Products.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "catalog_types",
                columns: new[] { "id", "type" },
                values: new object[,]
                {
                    { 1, "Appetizer" },
                    { 2, "Entree" },
                    { 3, "Dessert" },
                    { 4, "Bread" },
                    { 5, "Beverage" },
                    { 6, "Side" }
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("348cc1a0-707b-4e1b-9700-de40d54acd31"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { 1, "Cubes of fresh cottage cheese marinated in yogurt and aromatic spices, grilled to perfection in a tandoor oven. Served with fresh salad and mint chutney." });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { 2, "A spicy mashed vegetable curry served with buttery toasted bread rolls. This popular Mumbai street food is garnished with fresh onions, cilantro, and a squeeze of lemon." });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("970afb7e-5616-46c4-b3fa-f9abb62928db"),
                columns: new[] { "catalog_type_id", "description", "price" },
                values: new object[] { 1, "Crispy golden triangular pastries filled with spiced potatoes and peas. A beloved Indian street food appetizer served with mint and tamarind chutneys.", 8.99m });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("bc047267-5eb9-4681-b489-4473ccd00a87"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { 3, "A delightful homemade pie with a buttery crust filled with seasonal fruits and topped with a golden lattice. Served warm with vanilla ice cream." });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "id", "available_stock", "catalog_brand_id", "catalog_type_id", "category_name", "description", "image_url", "name", "price" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789001"), 0, null, 2, "Entree", "Tender pieces of tandoori chicken simmered in a rich, creamy tomato sauce with aromatic spices. A classic North Indian dish served with basmati rice.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Butter Chicken", 18.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789002"), 0, null, 2, "Entree", "Fragrant basmati rice layered with spiced chicken, caramelized onions, and saffron. Slow-cooked to perfection and served with cooling raita.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Chicken Biryani", 19.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789003"), 0, null, 4, "Bread", "Soft, pillowy flatbread brushed with garlic butter and fresh herbs. Baked in our traditional clay tandoor oven until perfectly golden.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Garlic Naan", 4.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789004"), 0, null, 2, "Entree", "Half chicken marinated overnight in yogurt and traditional spices, then roasted in a clay tandoor. Served sizzling with grilled vegetables.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Tandoori Chicken", 17.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789005"), 0, null, 2, "Entree", "Tender lamb pieces braised in a rich Kashmiri sauce with whole spices and aromatic herbs. A traditional recipe passed down through generations.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Lamb Rogan Josh", 22.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789006"), 0, null, 2, "Entree", "Fresh cottage cheese cubes in a vibrant spinach puree flavored with garlic and ginger. A nutritious vegetarian favorite.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Palak Paneer", 15.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789007"), 0, null, 5, "Beverage", "A refreshing yogurt-based drink blended with ripe Alphonso mangoes and a hint of cardamom. The perfect complement to any spicy meal.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Mango Lassi", 5.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789008"), 0, null, 3, "Dessert", "Soft milk dumplings deep-fried to golden perfection and soaked in rose-scented sugar syrup. Served warm with a drizzle of cream.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Gulab Jamun", 7.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789009"), 0, null, 2, "Entree", "Grilled chicken pieces in a luscious, creamy tomato-based curry sauce. Britain's favorite Indian dish, rich with warming spices.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Chicken Tikka Masala", 18.49m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789010"), 0, null, 2, "Entree", "Mixed seasonal vegetables simmered in a mild, creamy cashew and coconut sauce. A gentle introduction to Indian cuisine.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Vegetable Korma", 14.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789011"), 0, null, 1, "Appetizer", "Crispy deep-fried fritters made with sliced onions and gram flour, seasoned with cumin and coriander. Perfect as a starter or snack.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Onion Bhaji", 6.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789012"), 0, null, 2, "Entree", "Fresh catch of the day cooked in a tangy tamarind and coconut curry from the coastal regions of South India. Served with steamed rice.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Fish Curry", 20.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789013"), 0, null, 2, "Entree", "Black lentils slow-cooked overnight with butter and cream, creating a rich and hearty vegetarian dish. A Punjabi specialty.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Dal Makhani", 13.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789014"), 0, null, 1, "Appetizer", "Minced chicken mixed with fresh herbs and spices, molded onto skewers and grilled in tandoor. Served with mint chutney and onion rings.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Chicken Seekh Kebab", 14.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789015"), 0, null, 6, "Side", "Cool and refreshing yogurt-based condiment with cucumber, tomatoes, and aromatic spices. Essential for balancing spicy dishes.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Raita", 3.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789016"), 0, null, 2, "Entree", "Cauliflower and potatoes stir-fried with turmeric, cumin, and fresh ginger. A comforting dry vegetable dish from North India.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Aloo Gobi", 12.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789017"), 0, null, 2, "Entree", "Succulent tiger prawns cooked in a spicy masala sauce with onions, tomatoes, and aromatic coastal spices.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Prawn Masala", 23.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789018"), 0, null, 4, "Bread", "Fluffy naan bread stuffed with melted mozzarella and cheddar cheese. Baked until golden and served hot from the tandoor.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Cheese Naan", 5.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789019"), 0, null, 3, "Dessert", "Soft cheese patties soaked in sweetened, thickened milk flavored with cardamom and saffron. Garnished with pistachios.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Rasmalai", 8.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789020"), 0, null, 5, "Beverage", "Traditional Indian spiced tea brewed with cinnamon, cardamom, ginger, and cloves. Served with warm milk.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Masala Chai", 3.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789021"), 0, null, 2, "Entree", "Spiced minced lamb cooked with peas, onions, and tomatoes. A hearty dish perfect with naan or rice.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Lamb Keema", 19.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789022"), 0, null, 2, "Entree", "Chickpeas simmered in a tangy tomato-based sauce with traditional North Indian spices. A protein-rich vegetarian favorite.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Chana Masala", 12.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789023"), 0, null, 1, "Appetizer", "Crispy thin lentil wafers served with an assortment of chutneys. A traditional accompaniment to any Indian meal.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Pappadum", 2.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789024"), 0, null, 2, "Entree", "Slow-cooked goat meat in a rich, aromatic gravy with whole spices. A traditional recipe that melts in your mouth.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Mutton Curry", 24.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789025"), 0, null, 3, "Dessert", "Traditional Indian ice cream made with reduced milk, flavored with cardamom and pistachios. Denser and creamier than regular ice cream.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Kulfi", 6.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789026"), 0, null, 2, "Entree", "Tender chicken pieces cooked in a flavorful spinach and mustard green sauce with garlic and ginger.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Saag Chicken", 17.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789027"), 0, null, 2, "Entree", "Fragrant basmati rice layered with mixed vegetables, fried onions, and aromatic spices. A vegetarian feast in every bite.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Vegetable Biryani", 15.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789028"), 0, null, 2, "Entree", "Fiery Goan curry with tender lamb pieces in a tangy, spicy sauce made with vinegar and chili. For those who love heat!", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Lamb Vindaloo", 21.99m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789029"), 0, null, 6, "Side", "Sweet and tangy condiment made from ripe mangoes, sugar, and spices. The perfect accompaniment to any curry dish.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Mango Chutney", 2.49m },
                    { new Guid("a1b2c3d4-e5f6-4789-abcd-123456789030"), 0, null, 3, "Dessert", "Creamy Indian rice pudding slow-cooked with milk, sugar, and cardamom. Garnished with almonds, pistachios, and saffron strands.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Kheer", 6.99m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789001"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789002"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789003"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789004"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789005"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789006"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789007"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789008"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789009"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789010"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789011"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789012"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789013"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789014"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789015"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789016"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789017"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789018"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789019"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789020"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789021"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789022"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789023"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789024"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789025"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789026"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789027"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789028"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789029"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("a1b2c3d4-e5f6-4789-abcd-123456789030"));

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "catalog_types",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("348cc1a0-707b-4e1b-9700-de40d54acd31"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { null, "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium." });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { null, "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium." });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("970afb7e-5616-46c4-b3fa-f9abb62928db"),
                columns: new[] { "catalog_type_id", "description", "price" },
                values: new object[] { null, "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium.", 15m });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("bc047267-5eb9-4681-b489-4473ccd00a87"),
                columns: new[] { "catalog_type_id", "description" },
                values: new object[] { null, "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium." });
        }
    }
}
