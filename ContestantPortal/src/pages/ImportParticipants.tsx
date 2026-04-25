import { useState } from 'react';
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
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  List,
  ListItem,
  ListItemText,
  Chip,
} from '@mui/material';
import { ArrowBack, Upload, PersonAdd } from '@mui/icons-material';
import { contestService } from '../services/contestService';

export function ImportParticipants() {
  const { contestId } = useParams<{ contestId: string }>();
  const { theme } = useTheme();
  const navigate = useNavigate();
  const toast = useToast();
  const [loading, setLoading] = useState(false);
  const [emailText, setEmailText] = useState('');
  const [role, setRole] = useState<'contestant' | 'jury' | 'challenge_writer'>('contestant');
  const [result, setResult] = useState<any>(null);

  const handleImport = async () => {
    const emails = emailText
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0);

    if (emails.length === 0) {
      toast.error('Please enter at least one email address');
      return;
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const invalidEmails = emails.filter((email) => !emailRegex.test(email));
    if (invalidEmails.length > 0) {
      toast.error(`Invalid email format: ${invalidEmails.join(', ')}`);
      return;
    }

    try {
      setLoading(true);
      const importResult = await contestService.importParticipants(Number(contestId), {
        emails,
        role,
      });
      setResult(importResult);
      toast.success('Participants imported successfully');
    } catch (error: any) {
      toast.error(error.message || 'Failed to import participants');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setEmailText('');
    setResult(null);
  };

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box display="flex" alignItems="center" gap={2} mb={3}>
        <Button
          startIcon={<ArrowBack />}
          onClick={() => navigate(`/contest/${contestId}/challenges`)}
          sx={{ color: theme === 'dark' ? 'rgb(156, 163, 175)' : 'rgb(107, 114, 128)' }}
        >
          Back
        </Button>
      </Box>

      <Card>
        <CardContent sx={{ p: 4 }}>
          <Box display="flex" alignItems="center" gap={2} mb={3}>
            <PersonAdd sx={{ fontSize: 32, color: theme === 'dark' ? '#3b82f6' : '#2563eb' }} />
            <Typography variant="h5" fontWeight="bold">
              Import Participants
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 3 }}>
            Enter email addresses (one per line). Users will be automatically created if they don't
            exist in the system.
          </Alert>

          {!result ? (
            <>
              <TextField
                fullWidth
                multiline
                rows={10}
                label="Email Addresses"
                placeholder="user1@example.com&#10;user2@example.com&#10;user3@example.com"
                value={emailText}
                onChange={(e) => setEmailText(e.target.value)}
                sx={{ mb: 3 }}
              />

              <FormControl fullWidth sx={{ mb: 3 }}>
                <InputLabel>Role</InputLabel>
                <Select value={role} label="Role" onChange={(e) => setRole(e.target.value as any)}>
                  <MenuItem value="contestant">Contestant</MenuItem>
                  <MenuItem value="jury">Jury</MenuItem>
                  <MenuItem value="challenge_writer">Challenge Writer</MenuItem>
                </Select>
              </FormControl>

              <Box display="flex" gap={2} justifyContent="flex-end">
                <Button variant="outlined" onClick={() => navigate(`/contest/${contestId}/challenges`)}>
                  Cancel
                </Button>
                <Button
                  variant="contained"
                  startIcon={<Upload />}
                  onClick={handleImport}
                  disabled={loading || !emailText.trim()}
                  sx={{
                    bgcolor: theme === 'dark' ? '#3b82f6' : '#2563eb',
                    '&:hover': {
                      bgcolor: theme === 'dark' ? '#2563eb' : '#1d4ed8',
                    },
                  }}
                >
                  {loading ? 'Importing...' : 'Import Participants'}
                </Button>
              </Box>
            </>
          ) : (
            <>
              <Alert severity="success" sx={{ mb: 3 }}>
                Import completed successfully!
              </Alert>

              <Box sx={{ mb: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Import Summary
                </Typography>
                <List>
                  <ListItem>
                    <ListItemText
                      primary="Total Emails"
                      secondary={result.totalEmails}
                      primaryTypographyProps={{ fontWeight: 'medium' }}
                    />
                    <Chip label={result.totalEmails} color="default" />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="New Users Created"
                      secondary="Users that didn't exist in the system"
                      primaryTypographyProps={{ fontWeight: 'medium' }}
                    />
                    <Chip label={result.newUsersCreated} color="success" />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Existing Users Added"
                      secondary="Users that already existed"
                      primaryTypographyProps={{ fontWeight: 'medium' }}
                    />
                    <Chip label={result.existingUsersAdded} color="primary" />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Already Participants"
                      secondary="Users already in this contest"
                      primaryTypographyProps={{ fontWeight: 'medium' }}
                    />
                    <Chip label={result.alreadyParticipants} color="warning" />
                  </ListItem>
                  {result.failedEmails && result.failedEmails.length > 0 && (
                    <ListItem>
                      <ListItemText
                        primary="Failed"
                        secondary="Emails that couldn't be processed"
                        primaryTypographyProps={{ fontWeight: 'medium' }}
                      />
                      <Chip label={result.failedEmails.length} color="error" />
                    </ListItem>
                  )}
                </List>

                {result.failedEmails && result.failedEmails.length > 0 && (
                  <Alert severity="error" sx={{ mt: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Failed Emails:
                    </Typography>
                    <Typography variant="body2">{result.failedEmails.join(', ')}</Typography>
                  </Alert>
                )}
              </Box>

              <Box display="flex" gap={2} justifyContent="flex-end">
                <Button variant="outlined" onClick={handleReset}>
                  Import More
                </Button>
                <Button
                  variant="contained"
                  onClick={() => navigate(`/contest/${contestId}/challenges`)}
                  sx={{
                    bgcolor: theme === 'dark' ? '#3b82f6' : '#2563eb',
                    '&:hover': {
                      bgcolor: theme === 'dark' ? '#2563eb' : '#1d4ed8',
                    },
                  }}
                >
                  Done
                </Button>
              </Box>
            </>
          )}
        </CardContent>
      </Card>
    </Container>
  );
}
