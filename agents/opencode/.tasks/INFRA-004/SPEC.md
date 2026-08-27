# Behavioral Specification

## Requirement: Semantic agent routing

The infrastructure MUST expose the requested semantic agent IDs and route work by concrete complexity and risk rather than fixed numbered ensembles.

### Scenario: Routine application change

Given build and test evidence for a routine application change
When Discovery is selected
Then only the language `reviewer` is mandatory.

### Scenario: Architecture and contract risk

Given a change that materially affects frozen architecture and acceptance contracts
When Discovery is selected
Then `reviewer`, `guardian`, and `validator` are selected independently.

## Requirement: Provider routing and spend control

The infrastructure MUST use the requested OpenAI, direct DeepSeek, and OpenCode Go/Grok production channels without silent paid-provider fallback.

### Scenario: Normal worker execution

Given ordinary exploration, execution, engineering, testing, or validation work
When a worker model is selected
Then direct DeepSeek is used rather than OpenCode Go unless the semantic role requires Grok diversity.

### Scenario: Provider exhaustion

Given an assigned production provider is unavailable or quota-exhausted
When the agent cannot proceed
Then control returns to the coordinator or user without automatic substitution to another paid provider.
