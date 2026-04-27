import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTheme } from '../context/ThemeContext';
import { useToast } from '../hooks/useToast';
import {
  Box,
  Card,
  CardContent,
  Button,
  Typography,
  Container,
  Checkbox,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Chip,
  CircularProgress,
} from '@mui/material';
import { ArrowBack, Add, Settings } from '@mui/icons-material';
import { contestService } from '../services/contestService';
import type { BankChallenge, PullChallengeItem } from '../types/contestTypes';

export function PullChallenges() {
  const { contestId } = useParams<{ contestId: string }>();
  const { theme } = useTheme();
  const navigate = useNavigate();
  const toast = useToast();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [bankChallenges, setBankChallenges] = useState<BankChallenge[]>([]);
  const [selectedChallenges, setSelectedChallenges] = useState<Set<number>>(new Set());
  const [configDialogOpen, setConfigDialogOpen] = useState(false);
  const [currentConfig, setCurrentConfig] = useState<PullChallengeItem | null>(null);
  const [configurations, setConfigurations] = useState<Map<number, PullChallengeItem>>(new Map());

  useEffect(() => {
    loadBankChallenges();
  }, []);

  const loadBankChallenges = async () => {
    try {
      setLoading(true);
      const data = await contestService.getBankChallenges();
      setBankChallenges(data);
    } catch (error: any) {
      toast.error(error.message || 'Failed to load challenges');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectChallenge = (challengeId: number) => {
    const newSelected = new Set(selectedChallenges);
    if (newSelected.has(challengeId)) {
      newSelected.delete(challengeId);
      // Remove configuration if deselected
      const newConfigs = new Map(configurations);
      newConfigs.delete(challengeId);
      setConfigurations(newConfigs);
    } else {
      newSelected.add(challengeId);
    }
    setSelectedChallenges(newSelected);
  };

  const handleConfigureChallenge = (challenge: BankChallenge) => {
    const existingConfig = configurations.get(challenge.id);
    setCurrentConfig(
      existingConfig || {
        bankChallengeId: challenge.id,
        name: challenge.name,
        value: challenge.value,
        maxAttempts: challenge.maxAttempts,
        state: challenge.state,
        requireDeploy: challenge.requireDeploy,
        maxDeployCount: challenge.maxDeployCount,
        connectionProtocol: challenge.connectionProtocol,
      }
    );
    setConfigDialogOpen(true);
  };

  const handleSaveConfig = () => {
    if (currentConfig) {
      const newConfigs = new Map(configurations);
      newConfigs.set(currentConfig.bankChallengeId, currentConfig);
      setConfigurations(newConfigs);
      setConfigDialogOpen(false);
      toast.success('Configuration saved');
    }
  };

  const handlePullChallenges = async () => {
    if (selectedChallenges.size === 0) {
      toast.error('Please select at least one challenge');
      return;
    }

    try {
      setSubmitting(true);
      const challenges: PullChallengeItem[] = Array.from(selectedChallenges).map((id) => {
        const config = configurations.get(id);
        if (config) {
          return config;
        }
        // Use default values from bank
        const bankChallenge = bankChallenges.find((c) => c.id === id)!;
        return {
          bankChallengeId: id,
          name: bankChallenge.name,
          value: bankChallenge.value,
          maxAttempts: bankChallenge.maxAttempts,
          state: bankChallenge.state,
          requireDeploy: bankChallenge.requireDeploy,
          maxDeployCount: bankChallenge.maxDeployCount,
          connectionProtocol: bankChallenge.connectionProtocol,
        };
      });

      await contestService.pullChallengesToContest(Number(contestId), { challenges });
      toast.success(`Successfully pulled ${challenges.length} challenges`);
      navigate(`/contest/${contestId}/challenges`);
    } catch (error: any) {
      toast.error(error.message || 'Failed to pull challenges');
    } finally {
      setSubmitting(false);
    }
  };

  const isConfigured = (challengeId: number) => configurations.has(challengeId);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <Box display="flex" alignItems="center" justifyContent="space-between" mb={3}>
        <Box display="flex" alignItems="center" gap={2}>
          <Button
            startIcon={<ArrowBack />}
            onClick={() => navigate(`/contest/${contestId}/challenges`)}
            sx={{ color: theme === 'dark' ? 'rgb(156, 163, 175)' : 'rgb(107, 114, 128)' }}
          >
            Back
          </Button>
          <Typography variant="h5" fontWeight="bold">
            Pull Challenges from Bank
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={handlePullChallenges}
          disabled={selectedChallenges.size === 0 || submitting}
          sx={{
            bgcolor: theme === 'dark' ? '#3b82f6' : '#2563eb',
            '&:hover': {
              bgcolor: theme === 'dark' ? '#2563eb' : '#1d4ed8',
            },
          }}
        >
          {submitting ? 'Pulling...' : `Pull ${selectedChallenges.size} Challenge(s)`}
        </Button>
      </Box>

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell padding="checkbox">
                    <Checkbox
                      indeterminate={
                        selectedChallenges.size > 0 &&
                        selectedChallenges.size < bankChallenges.length
                      }
                      checked={
                        bankChallenges.length > 0 &&
                        selectedChallenges.size === bankChallenges.length
                      }
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedChallenges(new Set(bankChallenges.map((c) => c.id)));
                        } else {
                          setSelectedChallenges(new Set());
                          setConfigurations(new Map());
                        }
                      }}
                    />
                  </TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Category</TableCell>
                  <TableCell>Value</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>State</TableCell>
                  <TableCell>Deploy</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bankChallenges.map((challenge) => (
                  <TableRow
                    key={challenge.id}
                    hover
                    sx={{
                      bgcolor: isConfigured(challenge.id)
                        ? theme === 'dark'
                          ? 'rgba(59, 130, 246, 0.1)'
                          : 'rgba(37, 99, 235, 0.05)'
                        : 'inherit',
                    }}
                  >
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={selectedChallenges.has(challenge.id)}
                        onChange={() => handleSelectChallenge(challenge.id)}
                      />
                    </TableCell>
                    <TableCell>
                      <Typography fontWeight="medium">{challenge.name}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={challenge.category || 'N/A'} size="small" />
                    </TableCell>
                    <TableCell>{challenge.value || 0}</TableCell>
                    <TableCell>{challenge.type || 'standard'}</TableCell>
                    <TableCell>
                      <Chip
                        label={challenge.state}
                        size="small"
                        color={challenge.state === 'visible' ? 'success' : 'default'}
                      />
                    </TableCell>
                    <TableCell>
                      {challenge.requireDeploy ? (
                        <Chip label="Yes" size="small" color="primary" />
                      ) : (
                        <Chip label="No" size="small" variant="outlined" />
                      )}
                    </TableCell>
                    <TableCell align="center">
                      <Button
                        size="small"
                        startIcon={<Settings />}
                        onClick={() => handleConfigureChallenge(challenge)}
                        disabled={!selectedChallenges.has(challenge.id)}
                        variant={isConfigured(challenge.id) ? 'contained' : 'outlined'}
                      >
                        {isConfigured(challenge.id) ? 'Configured' : 'Configure'}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          {bankChallenges.length === 0 && (
            <Box textAlign="center" py={4}>
              <Typography color="text.secondary">No challenges available in bank</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Configuration Dialog */}
      <Dialog
        open={configDialogOpen}
        onClose={() => setConfigDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Configure Challenge</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Challenge Name"
                  value={currentConfig?.name || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, name: e.target.value })
                  }
                  helperText="Leave empty to use bank default"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Points Value"
                  type="number"
                  value={currentConfig?.value || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, value: Number(e.target.value) })
                  }
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Max Attempts"
                  type="number"
                  value={currentConfig?.maxAttempts || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, maxAttempts: Number(e.target.value) })
                  }
                  helperText="0 = unlimited"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>State</InputLabel>
                  <Select
                    value={currentConfig?.state || 'visible'}
                    label="State"
                    onChange={(e) =>
                      setCurrentConfig((prev) => prev && { ...prev, state: e.target.value })
                    }
                  >
                    <MenuItem value="visible">Visible</MenuItem>
                    <MenuItem value="hidden">Hidden</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Time Limit (minutes)"
                  type="number"
                  value={currentConfig?.timeLimit || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, timeLimit: Number(e.target.value) })
                  }
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Cooldown (seconds)"
                  type="number"
                  value={currentConfig?.cooldown || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, cooldown: Number(e.target.value) })
                  }
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Max Deploy Count"
                  type="number"
                  value={currentConfig?.maxDeployCount || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, maxDeployCount: Number(e.target.value) })
                  }
                  helperText="0 = unlimited"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>Connection Protocol</InputLabel>
                  <Select
                    value={currentConfig?.connectionProtocol || 'http'}
                    label="Connection Protocol"
                    onChange={(e) =>
                      setCurrentConfig((prev) => prev && { ...prev, connectionProtocol: e.target.value })
                    }
                  >
                    <MenuItem value="http">HTTP</MenuItem>
                    <MenuItem value="tcp">TCP</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Connection Info"
                  multiline
                  rows={2}
                  value={currentConfig?.connectionInfo || ''}
                  onChange={(e) =>
                    setCurrentConfig((prev) => prev && { ...prev, connectionInfo: e.target.value })
                  }
                  helperText="Custom connection information"
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfigDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveConfig} variant="contained">
            Save Configuration
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
