using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Data
{
    public sealed class EventModelConverter : JsonConverter<EventModel>
    {
        private static readonly V2DateTimeOffsetConverter _dateTimeOffsetConverter = new V2DateTimeOffsetConverter();
        private static readonly EventTypeConverter _eventTypeConverter = new EventTypeConverter();

        public override EventModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var innerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var eventModel = new EventModel();
            if (reader.TokenType != JsonTokenType.StartObject)
                Throw();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    Throw();
                switch (reader.GetString())
                {
                    case "eventId":
                        if (!reader.Read())
                            Throw();
                        eventModel.EventId = reader.GetInt32();
                        break;
                    case "eventDate":
                        if (!reader.Read())
                            Throw();
                        eventModel.EventDate = _dateTimeOffsetConverter.Read(ref reader, typeof(DateTimeOffset), innerOptions);
                        break;
                    case "eventType":
                        if (!reader.Read())
                            Throw();
                        eventModel.EventType = _eventTypeConverter.Read(ref reader, typeof(EventType), innerOptions);
                        break;
                    case "eventData":
                        if (!reader.Read())
                            Throw();
                        switch (eventModel.EventType)
                        {
                            case EventType.CategoryChange:
                                eventModel.EventData = JsonSerializer.Deserialize<CategoryChangeEventDataModel>(ref reader, innerOptions);
                                break;
                            case EventType.LolCountsUpdate:
                                eventModel.EventData = JsonSerializer.Deserialize<LolCountsUpdateEventDataModel>(ref reader, innerOptions);
                                break;
                            case EventType.NewPost:
                                eventModel.EventData = JsonSerializer.Deserialize<NewPostEventDataModel>(ref reader, innerOptions);
                                break;
                            case EventType.PostChange:
                                eventModel.EventData = JsonSerializer.Deserialize<PostChangeEventDataModel>(ref reader, innerOptions);
                                break;
                            case EventType.ReadStatusUpdate:
                                eventModel.EventData = JsonSerializer.Deserialize<ReadStatusUpdateEventDataModel>(ref reader, innerOptions);
                                break;
                            default:
                                Throw();
                                break;
                        }
                        break;
                }
            }
            return eventModel;

            static void Throw() => throw new ParsingException("Invalid JSON.");
        }

        public override void Write(Utf8JsonWriter writer, EventModel value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("eventId", value.EventId);
            writer.WritePropertyName("eventDate");
            _dateTimeOffsetConverter.Write(writer, value.EventDate, options);
            writer.WritePropertyName("eventType");
            _eventTypeConverter.Write(writer, value.EventType, options);
            writer.WritePropertyName("eventData");
            value.EventData.Write(writer);
            writer.WriteEndObject();
        }
    }
}
