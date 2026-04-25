import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { useToast } from '../hooks/useToast';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Chip,
  CircularProgress,
  Container,
} from '@mui/material';
import {
  Add as AddIcon,
  EmojiEvents,
  People,
  Assignment,
  CalendarToday,
} from '@mui/icons-material';
import { contestService } from '../services/contestService';
import type { Contest } from '../types/contestTypes';

export function ContestList() {
  const { user, selectContest } = useAuth();
  const { theme } = useTheme();
  const navigate = useNavigate();
  const toast = useToast();
  const [contests, setContests] = useState<Contest[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    console.log('[ContestList] Component mounted');
    console.log('[ContestList] Token in localStorage:', localStorage.getItem('auth_token') ? 'exists' : 'missing');
    console.log('[ContestList] User in localStorage:', localStorage.getItem('user_info') ? 'exists' : 'missing');
    loadContests();
  }, []);

  const loadContests = async () => {
    try {
      console.log('[ContestList] Loading contests...');
      setLoading(true);
      const data = await contestService.getAllContests();
      console.log('[ContestList] Contests loaded successfully:', data.length);
      setContests(data);
    } catch (error: any) {
      console.error('[ContestList] Failed to load contests:', error);
      toast.error(error.message || 'Failed to load contests');
    } finally {
      setLoading(false);
    }
  };

  const getStateColor = (state: string) => {
    switch (state) {
      case 'visible':
        return 'success';
      case 'draft':
        return 'default';
      case 'paused':
        return 'warning';
      case 'ended':
        return 'error';
      default:
        return 'default';
    }
  };

  const formatDate = (date: string | null) => {
    if (!date) return 'Not set';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const handleContestClick = async (contestId: number) => {
    try {
      console.log('[ContestList] Contest clicked, ID:', contestId);
      console.log('[ContestList] Token before selectContest:', localStorage.getItem('auth_token') ? 'exists' : 'missing');
      
      // Select contest and get new JWT token with contestId
      await selectContest(contestId);
      
      console.log('[ContestList] selectContest completed');
      console.log('[ContestList] Token after selectContest:', localStorage.getItem('auth_token') ? 'exists' : 'missing');
      
      toast.success('Contest selected');
      
      // Navigate to contest dashboard
      console.log('[ContestList] Navigating to:', `/contest/${contestId}/challenges`);
      navigate(`/contest/${contestId}/challenges`);
    } catch (error: any) {
      console.error('[ContestList] Error selecting contest:', error);
      toast.error(error.message || 'Failed to select contest');
    }
  };

  const handleCreateContest = () => {
    navigate('/contest/create');
  };

  if (loading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="60vh"
      >
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={4}
      >
        <Typography variant="h4" fontWeight="bold">
          Contests
        </Typography>
        {(user?.type === 'admin' || user?.type === 'teacher') && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreateContest}
            sx={{
              bgcolor: theme === 'dark' ? '#3b82f6' : '#2563eb',
              '&:hover': {
                bgcolor: theme === 'dark' ? '#2563eb' : '#1d4ed8',
              },
            }}
          >
            Create Contest
          </Button>
        )}
      </Box>

      {contests.length === 0 ? (
        <Card>
          <CardContent>
            <Box textAlign="center" py={4}>
              <EmojiEvents sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h6" color="text.secondary">
                No contests available
              </Typography>
              {(user?.type === 'admin' || user?.type === 'teacher') && (
                <Button
                  variant="outlined"
                  startIcon={<AddIcon />}
                  onClick={handleCreateContest}
                  sx={{ mt: 2 }}
                >
                  Create Your First Contest
                </Button>
              )}
            </Box>
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={3}>
          {contests.map((contest) => (
            <Grid item xs={12} md={6} lg={4} key={contest.id}>
              <Card
                sx={{
                  height: '100%',
                  cursor: 'pointer',
                  transition: 'transform 0.2s, box-shadow 0.2s',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: theme === 'dark' ? '0 8px 24px rgba(0,0,0,0.4)' : '0 8px 24px rgba(0,0,0,0.1)',
                  },
                }}
              >
                <CardContent onClick={() => handleContestClick(contest.id)}>
                  <Box display="flex" justifyContent="space-between" alignItems="start" mb={2}>
                    <Typography variant="h6" fontWeight="bold" sx={{ flex: 1 }}>
                      {contest.name}
                    </Typography>
                    <Chip
                      label={contest.state}
                      color={getStateColor(contest.state)}
                      size="small"
                    />
                  </Box>

                  {contest.description && (
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{
                        mb: 2,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                      }}
                    >
                      {contest.description}
                    </Typography>
                  )}

                  <Box display="flex" flexDirection="column" gap={1}>
                    <Box display="flex" alignItems="center" gap={1}>
                      <CalendarToday sx={{ fontSize: 16, color: 'text.secondary' }} />
                      <Typography variant="caption" color="text.secondary">
                        Start: {formatDate(contest.startTime)}
                      </Typography>
                    </Box>

                    <Box display="flex" alignItems="center" gap={1}>
                      <CalendarToday sx={{ fontSize: 16, color: 'text.secondary' }} />
                      <Typography variant="caption" color="text.secondary">
                        End: {formatDate(contest.endTime)}
                      </Typography>
                    </Box>

                    <Box display="flex" gap={2} mt={1}>
                      <Box display="flex" alignItems="center" gap={0.5}>
                        <Assignment sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">
                          {contest.challengeCount} challenges
                        </Typography>
                      </Box>

                      <Box display="flex" alignItems="center" gap={0.5}>
                        <People sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">
                          {contest.participantCount} participants
                        </Typography>
                      </Box>
                    </Box>

                    {contest.semesterName && (
                      <Chip
                        label={contest.semesterName}
                        size="small"
                        variant="outlined"
                        sx={{ mt: 1, width: 'fit-content' }}
                      />
                    )}
                  </Box>
                </CardContent>

                {/* Admin Actions */}
                {(user?.type === 'admin' || contest.ownerId === user?.id) && (
                  <Box
                    sx={{
                      borderTop: theme === 'dark' ? '1px solid rgb(55, 65, 81)' : '1px solid rgb(229, 231, 235)',
                      p: 2,
                      display: 'flex',
                      gap: 1,
                      flexWrap: 'wrap',
                    }}
                    onClick={(e) => e.stopPropagation()}
                  >
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => navigate(`/contest/${contest.id}/pull-challenges`)}
                    >
                      Pull Challenges
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => navigate(`/contest/${contest.id}/import-participants`)}
                    >
                      Import Users
                    </Button>
                  </Box>
                )}
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
