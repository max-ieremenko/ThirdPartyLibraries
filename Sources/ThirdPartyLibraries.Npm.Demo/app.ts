import chalk from 'chalk';

const log = console.log;

log(chalk.blue('Hello') + ' World' + chalk.red('!'));

log(`
CPU: ${chalk.red('90%')}
RAM: ${chalk.green('40%')}
DISK: ${chalk.yellow('70%')}
`);

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));
await delay(1000);