const Configuration = {
    extends: ['@commitlint/config-conventional'],
    parserPreset: {
        parserOpts: {
            issuePrefixes: ['LT-']
        }
    },
    formatter: '@commitlint/format',
    rules: {
        'references-empty': [2, 'never']
    },
};

module.exports = Configuration;