using System;
using System.Collections.Generic;
using FlowIoC.Editor.DependencyDiagram.Data;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.Simulation
{
    public class SimulationController
    {
        public enum SimulationState
        {
            Stopped,
            Playing,
            Paused
        }
        
        public SimulationState State { get; private set; } = SimulationState.Stopped;
        
        public DiagramGraph Graph { get; private set; }
        public int CurrentStep { get; private set; }
        
        private List<SimulationStep> _steps = new List<SimulationStep>();
        private float _simulationSpeed = 1.0f;
        private bool _loopSimulation = false;
        
        public event Action<int> OnStepChanged;
        public event Action<SimulationState> OnStateChanged;
        public event Action<DiagramNode> OnNodeActivated;
        public event Action<DiagramEdge> OnEdgeActivated;
        public event Action OnSimulationReset;
        public event Action OnSimulationCompleted;
        
        public void Initialize(DiagramGraph graph)
        {
            Graph = graph;
            Reset();
        }
        
        public void Play()
        {
            if (Graph == null || _steps.Count == 0) return;
            
            if (State == SimulationState.Paused)
            {
                State = SimulationState.Playing;
                OnStateChanged?.Invoke(State);
            }
            else if (State == SimulationState.Stopped)
            {
                Reset();
                State = SimulationState.Playing;
                OnStateChanged?.Invoke(State);
                
                // Start simulation coroutine or use EditorApplication.update
                // This is simplified for example purposes
                Debug.Log("Simulation started");
            }
        }
        
        public void Pause()
        {
            if (State == SimulationState.Playing)
            {
                State = SimulationState.Paused;
                OnStateChanged?.Invoke(State);
                Debug.Log("Simulation paused");
            }
        }
        
        public void Stop()
        {
            if (State != SimulationState.Stopped)
            {
                State = SimulationState.Stopped;
                OnStateChanged?.Invoke(State);
                Reset();
                Debug.Log("Simulation stopped");
            }
        }
        
        public void Step()
        {
            if (Graph == null || _steps.Count == 0) return;
            
            if (State == SimulationState.Stopped)
            {
                Reset();
            }
            
            if (CurrentStep < _steps.Count)
            {
                ExecuteStep(CurrentStep);
                CurrentStep++;
                OnStepChanged?.Invoke(CurrentStep);
                
                if (CurrentStep >= _steps.Count)
                {
                    OnSimulationCompleted?.Invoke();
                    
                    if (_loopSimulation)
                    {
                        Reset();
                    }
                    else
                    {
                        State = SimulationState.Stopped;
                        OnStateChanged?.Invoke(State);
                    }
                }
            }
        }
        
        public void SetSimulationSpeed(float speed)
        {
            _simulationSpeed = Mathf.Clamp(speed, 0.1f, 5.0f);
        }
        
        public void SetLoopSimulation(bool loop)
        {
            _loopSimulation = loop;
        }
        
        private void Reset()
        {
            CurrentStep = 0;
            GenerateSimulationSteps();
            OnSimulationReset?.Invoke();
            OnStepChanged?.Invoke(CurrentStep);
        }
        
        private void GenerateSimulationSteps()
        {
            _steps.Clear();
            
            if (Graph == null) return;
            
            // In a real implementation, this would analyze the diagram and create a sequence of steps
            // For this example, we'll just create dummy steps
            
            // TODO: Implement actual simulation step generation based on the graph
            // This would involve analyzing signal flows, command executions, etc.
        }
        
        private void ExecuteStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= _steps.Count) return;
            
            var step = _steps[stepIndex];
            
            if (step.ActivatedNode != null)
            {
                OnNodeActivated?.Invoke(step.ActivatedNode);
            }
            
            if (step.ActivatedEdge != null)
            {
                OnEdgeActivated?.Invoke(step.ActivatedEdge);
            }
        }
    }
} 