using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public sealed class SaleProfile : Profile
{
    public SaleProfile()
    {
        CreateMap<Sale, SaleModel>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

        CreateMap<SaleItem, SaleItemModel>();
    }
}