using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;


namespace NCoverCop
{
    public class NCoverResults : INCoverResults
    {
        private readonly double percentageCovered = 0.0;
        private readonly double total = 0.0;
        private readonly double totalVisited = 0.0;
        private readonly List<INCoverNode> unvisited = new List<INCoverNode>();

        public NCoverResults(IEnumerable<INCoverNode> nodes)
        {
            foreach (INCoverNode node in nodes)
            {
                if (!node.IsExcluded)
                {
                    total++;

                    if (node.IsVisited)
                    {
                        totalVisited++;
                    }
                    else
                    {
                        unvisited.Add(node);
                    }
                }
            }
            percentageCovered = Math.Round(totalVisited/total, 5);
        }

        #region INCoverResults Members

        public double PercentageCovered
        {
            get { return percentageCovered; }
        }

        public double Total
        {
            get { return total; }
        }

        public double TotalVisited
        {
            get { return totalVisited; }
        }

        public double TotalUnvisited
        {
            get { return Total - TotalVisited; }
        }

        public string ReportNewUntestedCode(INCoverResults previous)
        {
            List<INCoverNode> nodes = new List<INCoverNode>();
            foreach (INCoverNode node in unvisited)
            {
                if (!previous.HasMatchingUnvisitedNode(node))
                {
                    nodes.Add(node);
                }
            }

            INCoverNode lastNode = null;
            List<INCoverNode> condensed = new List<INCoverNode>();
            foreach (INCoverNode node in nodes)
            {
                if (lastNode == null)
                {
                    lastNode = node;
                }
                else
                {
                    if (node.Follows(lastNode))
                    {
                        lastNode = lastNode.ExtendWith(node);
                    }
                    else
                    {
                        condensed.Add(lastNode);
                        lastNode = node;
                    }
                }
            }
            if (lastNode != null) condensed.Add(lastNode);

            string output = "";
            foreach (INCoverNode node in condensed)
            {
                output += node + "\n";
            }
            return output;
        }

        public bool HasMatchingUnvisitedNode(INCoverNode node)
        {
            foreach (INCoverNode unvisitedNode in unvisited)
            {
                if (unvisitedNode.Matches(node))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        public static NCoverResults Open(string coverageFile, Regex partOfPathToKeep)
        {
            List<INCoverNode> nodes = new List<INCoverNode>();
            XmlDocument results = new XmlDocument();
            if (File.Exists(coverageFile))
            {
                try
                {
                    results.LoadXml(File.ReadAllText(coverageFile));
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                }

                foreach (XmlNode node in results.SelectNodes("//seqpnt"))
                {
                    nodes.Add(new NCoverNode(node, partOfPathToKeep));
                }
            }
            return new NCoverResults(nodes);
        }
    }
}