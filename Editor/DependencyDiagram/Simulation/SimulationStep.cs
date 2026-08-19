using FlowIoC.Editor.DependencyDiagram.Data;

namespace FlowIoC.Editor.DependencyDiagram.Simulation
{
    public class SimulationStep
    {
        public DiagramNode ActivatedNode { get; }
        public DiagramEdge ActivatedEdge { get; }
        public string Description { get; }
        public float Duration { get; }
        
        public SimulationStep(DiagramNode activatedNode, DiagramEdge activatedEdge, string description, float duration = 0.5f)
        {
            ActivatedNode = activatedNode;
            ActivatedEdge = activatedEdge;
            Description = description;
            Duration = duration;
        }
    }
} 