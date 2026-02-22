using System.Linq;

namespace RailConductor.GraphOld;

public class ValidateConnectionBuildPhase : TrackGraphBuildPhase
{
    public override int GraphBuildPhase => TrackBuildPhase.Validation;
    public override void ExecuteBuildPhase(Track track, TrackGraph graph)
    {
        foreach (var node in graph.GetNodes())
        {
            var links = graph.GetLinks(node.Id).ToList();
            
            switch (links.Count)
            {
                case 1:
                    node.IncomingLinks = [links[0]];
                    node.OutgoingLinks = [];
                    break;
                case 2:
                    node.IncomingLinks = [links[0]];
                    node.OutgoingLinks = [links[1]];
                    break;
            }
        }
    }
}