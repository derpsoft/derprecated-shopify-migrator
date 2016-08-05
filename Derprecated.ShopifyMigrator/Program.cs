﻿using System;
using System.Configuration;
using System.Linq;
using Derprecated.ShopifyMigrator.Models;
using Derprecated.ShopifyMigrator.Models.Route;
using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.MiniProfiler.Storage;
using ServiceStack.OrmLite;

namespace Derprecated.ShopifyMigrator
{
    internal class Program
    {
        static Program()
        {
            var license = FindLicense();
            if (!license.IsNullOrEmpty())
            {
                Licensing.RegisterLicense(license);
            }

            Container = new Container();
            Configure(Container);
        }

        private static Container Container { get; }

        private static string FindLicense()
        {
            var appSettings = new AppSettings();

            var license = Environment.GetEnvironmentVariable("ss.license") ?? appSettings.GetString("ss.license");

            return license;
        }

        private static void Configure(Container container)
        {
            var appSettings = new AppSettings();

            // DB
            container.Register<IDbConnectionFactory>(c =>
            {
                var connectionString =
                    ConfigurationManager.ConnectionStrings["AzureSql"].ConnectionString;

                return new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            });
            // Db filters
            OrmLiteConfig.InsertFilter = (dbCmd, row) =>
            {
                if (row is IAuditable)
                {
                    var auditRow = row as IAuditable;
                    auditRow.CreateDate = auditRow.ModifyDate = DateTime.UtcNow;
                }

                if (row is Product)
                {
                    var product = row as Product;
                    product.OnInsert();
                }
            };
            OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                if (row is IAuditable)
                {
                    var auditRow = row as IAuditable;
                    auditRow.ModifyDate = DateTime.UtcNow;
                }

                if (row is Product)
                {
                    var product = row as Product;
                    product.OnUpdate();
                }
            };

            container.Register(c =>
            {
                var domain = appSettings.Get("shopify.store.domain");
                var apiKey = appSettings.Get("shopify.api.key");
                var password = appSettings.Get("shopify.api.password");

                return new JsonServiceClient($"https://{domain}")
                {
                    UserName = apiKey,
                    Password = password
                };
            });


            using (var db = Container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<Product>();
                db.CreateTableIfNotExists<ProductMeta>();
            }
        }

        private static void Main(string[] args)
        {
            using (var client = Container.Resolve<JsonServiceClient>())
            using (var db = Container.Resolve<IDbConnectionFactory>().Open())
            {
                Console.WriteLine(@"Requesting latest product counts...");

                var shopifyCount = client.Get(new GetProductsCount());
                var dbCount = db.Count<Product>();

                Console.WriteLine($"Found\n\tShopify: {shopifyCount.Count}\n\tDb: {dbCount}\n");
                Console.WriteLine("Merging...\n");

                var shopifyProducts = client.Get(new GetProducts {Limit = shopifyCount.Count});

                var products = shopifyProducts.Products.Map(p =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var product = db.Where<Product>(new {ShopifyId = p.Id}).SingleOrDefault();

                    if (product == default(Product))
                    {
                        product = Product.From(p);
                        Console.WriteLine($"New [{product.ShopifyId}] {p.Title.Truncate(40)}...");
                    }
                    else
                    {
                        db.LoadReferences(product);
                        product.Merge(p);
                        Console.WriteLine($"Existing [{product.ShopifyId} -> {product.Id}] {p.Title.Truncate(40)}...");
                    }

                    return product;
                });

                Console.WriteLine("Saving...\n");

                // ReSharper disable once AccessToDisposedClosure
                var results = products.Map(p => db.Save(p, true)).Count();

                Console.WriteLine($"Saved {results} Products.\n");
            }
            Console.WriteLine(@"DONE");
            Console.WriteLine(@"Press any key to quit...");
            Console.ReadKey();
        }
    }
}