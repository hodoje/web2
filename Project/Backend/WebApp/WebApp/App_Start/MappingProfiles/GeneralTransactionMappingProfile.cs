﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp.Models.DomainModels;
using WebApp.Models.Dtos;

namespace WebApp.App_Start.MappingProfiles
{
	public class GeneralTransactionMappingProfile : Profile
	{
		public GeneralTransactionMappingProfile()
		{
			CreateMap<GeneralTransactionDto, GeneralTransaction>();
		}
	}
}