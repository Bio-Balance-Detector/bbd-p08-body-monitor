﻿using Fitbit.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fitbit.Api.Portable.Models
{
    public class HeartActivitiesIntradayConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var heartActivitiesIntraday = value as HeartActivitiesIntraday;

            //{
            writer.WriteStartObject();

            writer.WritePropertyName("ActivitiesHeart");
            writer.WriteValue(heartActivitiesIntraday.ActivitiesHeart);

            // "DatasetInterval" : "1"
            writer.WritePropertyName("DatasetInterval");
            writer.WriteValue(heartActivitiesIntraday.DatasetInterval);

            // "DatasetType" : "SecondsHeartrate"
            writer.WritePropertyName("DatasetType");
            writer.WriteValue(heartActivitiesIntraday.DatasetType);

            writer.WritePropertyName("Dataset");
            writer.WriteStartArray();
            foreach (var datasetInverval in heartActivitiesIntraday.Dataset)
            {
                // "Time" : "2008-09-22T14:01:54.9571247Z"
                writer.WritePropertyName("Time");
                writer.WriteValue(datasetInverval.TimeUtc.ToString("o"));

                // "Value": 1
                writer.WritePropertyName("Value");
                writer.WriteValue(datasetInverval.Value);

            }
            writer.WriteEndArray();

            //}
            writer.WriteEndObject();

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var properties = jsonObject.Properties().ToList();

            HeartActivitiesIntraday result = new HeartActivitiesIntraday();
            result.DatasetInterval = Convert.ToInt32(jsonObject["DatasetInterval"]);
            result.DatasetType = jsonObject["DatasetType"].ToString();
            result.Dataset = new List<DatasetInterval>();

            foreach (JToken item in jsonObject["Dataset"].Children())
            {
                result.Dataset.Add(new DatasetInterval()
                {
                    TimeUtc = DateTime.Parse(item["Time"].ToString()),
                    Value = Convert.ToInt32(item["Value"])
                });
            };

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(HeartActivitiesIntraday).IsAssignableFrom(objectType);
        }
    }
}
