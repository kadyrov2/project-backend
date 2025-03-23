using Microsoft.EntityFrameworkCore;
using Service.Data;
using Service.TGBot;
using StackExchange.Redis;
using System.Text.Json;
internal class Program
{

    private static void Main(string[] args)
    {
        TGBot _tGBot = new TGBot();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true; // ��� ��������� �������������� JSON
                options.JsonSerializerOptions.IgnoreNullValues = true; // ������������ null-��������
            });

        builder.Services.AddSingleton<IConnectionMultiplexer>(options =>
                        ConnectionMultiplexer.Connect(("127.0.0.1:6379")));

        builder.Services.AddScoped<ICache, Cache>();

        var configuration = builder.Configuration;
        builder.Services.AddDbContext<UserContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();

        _tGBot.Start();

    }
}
