﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using WebApp.Models.DomainModels;
using WebApp.Persistence.Repository;

namespace WebApp.Persistence.ModelRepositoryInterfaces
{
    public interface ITransportationLineRepository : IRepository<TransportationLine, int>
    {
		IEnumerable<TransportationLine> GetAllIncludeTransportationLineType();
		TransportationLine FindByIdIncludeRoutePoints(int id);
		TransportationLine FindByLineNumberIncludeRoutePoints(int lineNum);
    }
}