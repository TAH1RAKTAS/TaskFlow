namespace TaskFlow.Exceptions;

public class BusinessException : Exception
{
    // Business Rule ihlallerini temsil eden özel exception
    public BusinessException(string message)
        : base(message)
    {
        // Hata mesajını Exception sınıfına aktarır
    }
}