using DataTyped;

var productCatalog = await Json.Get<ProductCatalog>("https://dummyjson.com/products");

foreach (var product in productCatalog.Products)
    Console.WriteLine(product.Title);

