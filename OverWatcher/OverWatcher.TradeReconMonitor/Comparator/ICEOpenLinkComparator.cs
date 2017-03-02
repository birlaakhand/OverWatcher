﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OverWatcher.Common.HelperFunctions;
using OverWatcher.Common.Logging;

namespace OverWatcher.TradeReconMonitor.Core
{

    partial class ICEOpenLinkComparator
    {
        public List<DataTable> Diff(List<DataTable> iceList, List<DataTable> oracleList)
        {
            Logger.Info("Diff ICE and Oracle...");
            List<DataTable> diff = new List<DataTable>();
            foreach(DataTable dt in iceList)
            {
                SwapLegIDAndDealID(dt);
            }
            foreach (CompanyName c in Enum.GetValues(typeof(CompanyName)))
            {
                foreach(ProductType p in Enum.GetValues(typeof(ProductType)))
                {
                    DataTable ice = ICEResultFilter(iceList.Find(s => s.TableName == c.ToString() + p.ToString()));
                    DataTable oracle = oracleList.Find(s => s.TableName == c.ToString() + p.ToString());
                    diff.Add(SwapLegIDAndDealID(Diff(ice, oracle)));
                }
            }
            foreach (DataTable dt in iceList)
            {
                SwapLegIDAndDealID(dt);
            }
            ApplyFilter(diff);
            return diff;
        }

        public DataTable Diff(DataTable ice, DataTable oracle) //sort and diff
        {
            HelperFunctions.SortDataTable<int>(ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable<int>(oracle, "ICEDEALID", SortDirection.ASC);
            DataTable diff = ice.Clone();
            diff.TableName = diff.TableName + " OpenLink Missing";
            int oracleRowsCount = oracle.Rows.Count;
            int iceRowsCount = ice.Rows.Count;
            int oracleCount = 0;
            int iceCount = 0;
            while (oracleCount < oracleRowsCount && iceCount <iceRowsCount)
            {
                int oc = int.Parse(oracle.Rows[oracleCount]["ICEDEALID"].ToString());
                int ic = int.Parse(ice.Rows[iceCount]["Deal ID"].ToString());
                if (oc == ic)
                {
                    oracleCount++;
                    iceCount++;
                }
                else if(oc > ic)
                {
                    diff.ImportRow(ice.Rows[iceCount++]);
                }
                else
                {
                    oracleCount++;
                }
            }
            while(iceCount < iceRowsCount)
            {
                diff.ImportRow(ice.Rows[iceCount++]);
            }
            return diff;
        }

        public DataTable SwapLegIDAndDealID(DataTable ice)
        {
            foreach (DataRow row in ice.Rows)
            {
                if (row["Leg ID"] != DBNull.Value && !string.IsNullOrEmpty(row["Leg ID"].ToString()))
                {
                    string deal = row["Deal ID"].ToString();
                    string leg = row["Leg ID"].ToString();
                    row["Deal ID"] = leg;
                    row["Leg ID"] = deal;
                }
            }
            return ice;
        }

        public DataTable ICEResultFilter(DataTable ice)
        {
            LinkedList<DataRow> repeated = new LinkedList<DataRow>();
            int loop = 2; 
            do
            {
                for (int i = 0; i < ice.Rows.Count; i++)
                {
                    if(string.IsNullOrEmpty(ice.Rows[i]["Deal ID"]?.ToString()))
                        continue;
                    var match = repeated.Any(x => ice.Rows[i]["Deal ID"].ToString() ==
                                                 x["Link ID"].ToString()) ? ice.Rows[i] : null;
                    if (match != null)
                    {
                        ice.Rows.Remove(match);
                        repeated.Remove(match);
                        continue;
                    }
                    if (!string.IsNullOrEmpty(ice.Rows[i]["Link ID"]?.ToString()))
                    {
                        repeated.AddLast(ice.Rows[i]);
                    }
                }
            } while (--loop > 0);
            return ice;
        }
    }
}
