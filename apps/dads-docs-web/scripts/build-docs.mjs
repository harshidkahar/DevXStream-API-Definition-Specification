import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const appDirectory = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const repositoryRoot = path.resolve(appDirectory, '..', '..');
const cliProject = path.join(repositoryRoot, 'src', 'DevXStream.DADS.Cli', 'DevXStream.DADS.Cli.csproj');
const definition = path.join(repositoryRoot, 'samples', 'ecommerce.dads.json');
const dataDirectory = path.join(appDirectory, 'public', 'data');

function run(command, args, cwd = repositoryRoot) {
  const executable = command === 'npm' && process.platform === 'win32' ? 'npm.cmd' : command;
  const result = spawnSync(executable, args, { cwd, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}

run('dotnet', ['run', '--project', cliProject, '--framework', 'net8.0', '--', 'build-docs-data', definition, '--output', path.join(dataDirectory, 'ecommerce.docs.json')]);
run('dotnet', ['run', '--project', cliProject, '--framework', 'net8.0', '--', 'export-openapi', definition, '--output', path.join(dataDirectory, 'ecommerce.openapi.json')]);
run(process.execPath, [path.join(appDirectory, 'node_modules', '@angular', 'cli', 'bin', 'ng.js'), 'build'], appDirectory);
