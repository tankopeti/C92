using AutoMapper;
using Cloud9_2.Models;

namespace Cloud9_2.Profiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.Partner, opt => opt.MapFrom(src => src.Partner))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                .ReverseMap();

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ReverseMap();

            CreateMap<Partner, PartnerDto>().ReverseMap();
            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<Currency, Currency>().ReverseMap();
        }
    }
}