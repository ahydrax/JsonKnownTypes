﻿using AutoFixture.NUnit3;
using FluentAssertions;
using JsonKnownTypes.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace JsonKnownTypes.UnitTests
{
    public class WithBaseClass
    {
        private static string DiscriminatorName { get => "type"; }

        [Test]
        [AutoData]
        public void AutoBaseClass(BaseClass entity) 
            => There_is_right_discriminator(entity);

        [Test]
        [AutoData]
        public void AutoBaseClass1Heir(BaseClass1Heir entity)
            => There_is_right_discriminator(entity);

        [Test]
        [AutoData]
        public void AutoBaseClass2Heir(BaseClass2Heir entity)
            => There_is_right_discriminator(entity);

        [Test]
        [AutoData]
        public void AutoBaseClass3Heir(BaseClass3Heir entity)
            => There_is_right_discriminator(entity);

        private void There_is_right_discriminator(BaseClass entity)
        {
            JsonKnownSettingsService.DiscriminatorAttribute = new JsonKnownDiscriminatorAttribute
            {
                Name = "type"
            };

            var a = JsonConvert.SerializeObject(entity, new JsonKnownTypeConverter<BaseClass>());

            var json = JsonConvert.SerializeObject(entity);
            var jObject = JToken.Parse(json);

            var discriminator = jObject[DiscriminatorName]?.Value<string>();
            var obj = JsonConvert.DeserializeObject<BaseClass>(json);

            obj.Should().BeEquivalentTo(entity);
            Assert.AreEqual(discriminator, entity.GetType().Name);
        }
    }

    [JsonConverter(typeof(JsonKnownTypeConverter<BaseClass>))]
    public class BaseClass
    {
        public string Summary { get; set; }
    }

    public class BaseClass1Heir : BaseClass
    {
        public int SomeInt { get; set; }
    }

    public class BaseClass2Heir : BaseClass
    {
        public double SomeDouble { get; set; }
        public string Detailed { get; set; }
    }

    public class BaseClass3Heir : BaseClass2Heir
    {
        public string SomeString { get; set; }
    }
}