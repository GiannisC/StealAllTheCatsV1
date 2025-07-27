namespace StealAllTheCats.Models
{
    public interface IRabbitMQPublisher
    {
        void Publish(FetchCatsCommand command);
    }
}
