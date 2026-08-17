using Ambev.DeveloperEvaluation.Application.Sales.CancelItem;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Clock;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IClock, SystemClock>();

        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();

        builder.Services.AddSingleton<IDiscountPolicy, ProgressiveDiscountPolicy>();

        builder.Services.AddScoped<ISaleEventPublisher, LogSaleEventPublisher>();

        builder.Services.AddScoped<IValidator<CreateSaleCommand>, CreateSaleValidator>();
        builder.Services.AddScoped<IValidator<UpdateSaleCommand>, UpdateSaleValidator>();
        builder.Services.AddScoped<IValidator<GetSaleCommand>, GetSaleValidator>();
        builder.Services.AddScoped<IValidator<DeleteSaleCommand>, DeleteSaleValidator>();
        builder.Services.AddScoped<IValidator<CancelSaleCommand>, CancelSaleValidator>();
        builder.Services.AddScoped<IValidator<CancelItemCommand>, CancelItemValidator>();
    }
}