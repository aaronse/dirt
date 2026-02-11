const commands = [
	{
		name: 'treemap',
		description: 'Generate a treemap visualization of the repository',
	},
	{
		name: 't',
		description: 'Alias for /treemap - Generate a treemap visualization',
	},
	{
		name: 'analyze',
		description: 'Analyze the repository',
	},
	{
		name: 'a',
		description: 'Alias for /analyze - Analyze the repository',
	},
];

// Parse CLI arguments - treat 't' as alias for 'treemap'
const args = process.argv.slice(2);
const command = args[0];

// Handle command aliases
if (command === 't' || command === 'treemap') {
	// Generate treemap visualization
	// ...existing treemap generation code...
} else {
	// ...existing code...
}