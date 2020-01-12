using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Repository.Template
{
    [TestFixture]
    public class ApplicationTest
    {
        [Test]
        public void DoNotSerializeDefaultValues()
        {
            var app = new Application
            {
                Name = "app name",
                InternalOnly = false
            };

            var json = JsonSerialize(app);
            Console.WriteLine(json);

            json.ShouldNotContain(nameof(Application.TargetFrameworks));
            json.ShouldNotContain(nameof(Application.Dependencies));
        }

        [Test]
        public void Serialize()
        {
            var app = new Application
            {
                Name = "app name",
                InternalOnly = false,
                TargetFrameworks = new[] { "f1" },
                Dependencies = { new LibraryDependency() }
            };

            var json = JsonSerialize(app);
            Console.WriteLine(json);

            json.ShouldContain(nameof(Application.TargetFrameworks));
            json.ShouldContain(nameof(Application.Dependencies));
        }

        private static string JsonSerialize(object instance)
        {
            var json = new StringBuilder();
            using (var jsonWriter = new JsonTextWriter(new StringWriter(json)))
            {
                new JsonSerializer().Serialize(jsonWriter, instance);
            }

            return json.ToString();
        }
    }
}
