﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebApp.Models.DomainModels;
using WebApp.Persistence.ModelRepositoryInterfaces;
using WebApp.Persistence.Repository;

namespace WebApp.Persistence.ModelRepositories
{
	public class TicketTypePricelistRepository : Repository<TicketTypePricelist, int>, ITicketTypePricelistRepository
	{
		protected ApplicationDbContext ApplicationDbContext
		{
			get { return context as ApplicationDbContext; }
		}

		public TicketTypePricelistRepository(DbContext context) : base(context)
		{
		}
	}
}