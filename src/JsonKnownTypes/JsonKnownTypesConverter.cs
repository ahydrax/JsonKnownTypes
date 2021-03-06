﻿using System;
using System.Threading;
using JsonKnownTypes.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonKnownTypes
{
    /// <summary>
    /// Convert json using discriminator
    /// </summary>
    public class JsonKnownTypesConverter<T> : JsonConverter
    {
        private readonly DiscriminatorValues _typesDiscriminatorValues
            = JsonKnownTypesSettingsManager.GetDiscriminatorValues<T>();

        public override bool CanConvert(Type objectType)
            => _typesDiscriminatorValues.Contains(objectType);

        private readonly ThreadLocal<bool> _isInRead = new ThreadLocal<bool>();

        public override bool CanRead => !IsInReadAndReset();

        private bool IsInReadAndReset()
        {
            if (_isInRead.Value)
            {
                _isInRead.Value = false;
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var jObject = JObject.Load(reader);

            var discriminator = jObject[_typesDiscriminatorValues.FieldName]?.Value<string>();

            if (_typesDiscriminatorValues.TryGetType(discriminator, out var typeForObject))
            {
                var jsonReader = jObject.CreateReader();

                if (objectType == typeForObject)
                    _isInRead.Value = true;

                var obj = serializer.Deserialize(jsonReader, typeForObject);

                return obj;
            }

            var discriminatorName = string.IsNullOrWhiteSpace(discriminator) ? "<empty-string>" : discriminator;
            throw new JsonKnownTypesException($"{discriminatorName} discriminator is not registered for {nameof(T)} type");
        }

        private readonly ThreadLocal<bool> _isInWrite = new ThreadLocal<bool>();

        public override bool CanWrite => !IsInWriteAndReset();

        private bool IsInWriteAndReset()
        {
            if (_isInWrite.Value)
            {
                _isInWrite.Value = false;
                return true;
            }
            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var objectType = value.GetType();

            if (_typesDiscriminatorValues.FallbackType != null && objectType == _typesDiscriminatorValues.FallbackType)
            {
                _isInWrite.Value = true;
                
                serializer.Serialize(writer, value, objectType);
                return;
            }
            
            if (_typesDiscriminatorValues.TryGetDiscriminator(objectType, out var discriminator))
            {
                _isInWrite.Value = true;

                var writerProxy = new JsonKnownProxyWriter(_typesDiscriminatorValues.FieldName, discriminator, writer);
                serializer.Serialize(writerProxy, value, objectType);
            }
            else
            {
                throw new JsonKnownTypesException($"There is no discriminator for {objectType.Name} type");
            }
        }
    }
}
