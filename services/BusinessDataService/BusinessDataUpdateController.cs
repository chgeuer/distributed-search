﻿namespace Mercury.Services.SearchService;

using Mercury.BusinessDataPump;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static Fundamentals.Types;
using static Mercury.Customer.Fashion.BusinessData;

[ApiController]
[Route("[controller]")]
public class BusinessDataUpdateController : ControllerBase
{
    private readonly BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate> businessDataProvider;

    public BusinessDataUpdateController(BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate> businessDataProvider)
    {
        this.businessDataProvider = businessDataProvider;
    }

    [HttpPost]
    public async Task<(Watermark, FashionBusinessData)> Post(FashionBusinessDataUpdate bdu)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        return (Watermark.NewWatermark(-1), null);
    }

    [HttpGet]
    public async Task<FashionBusinessData> Get(Watermark watermark)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        throw new NotSupportedException();
    }
}