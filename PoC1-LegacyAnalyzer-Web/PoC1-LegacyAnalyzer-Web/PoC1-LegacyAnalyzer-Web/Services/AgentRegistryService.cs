using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Services.AI;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for managing and retrieving specialist agents using a factory pattern.
    /// </summary>
    public class AgentRegistryService : IAgentRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentRegistryService> _logger;
        private readonly Dictionary<string, Type> _agentTypeRegistry;

        public AgentRegistryService(IServiceProvider serviceProvider, ILogger<AgentRegistryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Initialize agent type registry
            _agentTypeRegistry = new Dictionary<string, Type>
            {
                { "security", typeof(SecurityAnalystAgent) },
                { "performance", typeof(PerformanceAnalystAgent) },
                { "architecture", typeof(ArchitecturalAnalystAgent) }
            };
        }

        public ISpecialistAgentService? GetAgent(string specialty)
        {
            if (string.IsNullOrWhiteSpace(specialty))
            {
                _logger.LogWarning("GetAgent called with null or empty specialty");
                return null;
            }

            var normalizedSpecialty = specialty.ToLower();
            if (!_agentTypeRegistry.TryGetValue(normalizedSpecialty, out var agentType))
            {
                _logger.LogWarning("No agent registered for specialty: {Specialty}", specialty);
                return null;
            }

            try
            {
                var agent = _serviceProvider.GetService(agentType) as ISpecialistAgentService;
                if (agent == null)
                {
                    _logger.LogError("Failed to resolve agent of type {AgentType} for specialty: {Specialty}", agentType.Name, specialty);
                }
                return agent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving agent for specialty: {Specialty}", specialty);
                return null;
            }
        }

        public IEnumerable<ISpecialistAgentService> GetAllAgents()
        {
            var agents = new List<ISpecialistAgentService>();
            foreach (var kvp in _agentTypeRegistry)
            {
                var agent = GetAgent(kvp.Key);
                if (agent != null)
                {
                    agents.Add(agent);
                }
            }
            return agents;
        }

        public bool IsRegistered(string specialty)
        {
            if (string.IsNullOrWhiteSpace(specialty))
                return false;

            return _agentTypeRegistry.ContainsKey(specialty.ToLower());
        }

        public ISpecialistAgentService? GetAgentByName(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName))
            {
                _logger.LogWarning("GetAgentByName called with null or empty agentName");
                return null;
            }

            // Search through all registered agents to find one with matching name
            foreach (var agent in GetAllAgents())
            {
                if (agent.AgentName.Equals(agentName, StringComparison.OrdinalIgnoreCase))
                {
                    return agent;
                }
            }

            _logger.LogWarning("No agent found with name: {AgentName}", agentName);
            return null;
        }
    }
}

