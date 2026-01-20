using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    /// <summary>
    /// Defines a service for managing and retrieving specialist agents by specialty.
    /// </summary>
    public interface IAgentRegistry
    {
        /// <summary>
        /// Gets a specialist agent by specialty name.
        /// </summary>
        /// <param name="specialty">The specialty of the agent (e.g., "security", "performance", "architecture").</param>
        /// <returns>The specialist agent service, or null if not found.</returns>
        ISpecialistAgentService? GetAgent(string specialty);

        /// <summary>
        /// Gets all registered specialist agents.
        /// </summary>
        /// <returns>A collection of all registered specialist agents.</returns>
        IEnumerable<ISpecialistAgentService> GetAllAgents();

        /// <summary>
        /// Checks if an agent is registered for the specified specialty.
        /// </summary>
        /// <param name="specialty">The specialty to check.</param>
        /// <returns>True if an agent is registered for the specialty, false otherwise.</returns>
        bool IsRegistered(string specialty);

        /// <summary>
        /// Gets an agent by its agent name.
        /// </summary>
        /// <param name="agentName">The name of the agent.</param>
        /// <returns>The specialist agent service, or null if not found.</returns>
        ISpecialistAgentService? GetAgentByName(string agentName);
    }
}

