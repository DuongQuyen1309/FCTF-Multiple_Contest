import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '../context/ThemeContext';
import { useToast } from '../hooks/useToast';
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Container,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
} from '@mui/material';
import { ArrowBack, Save } from '@mui/icons-material';
import { contestService } from '../services/contestService';
import type { CreateContestPayload } from '../types/contestTypes';

export function CreateContest() {
  const { theme } = useTheme();
  const navigate = useNavigate();
  const toast = useToast();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState<CreateContestPayload>({
    name: '',
    description: '',
    slug: '',
    semesterName: '',
    startTime: '',
    endTime: '',
    userMode: 'users',
  });

  const handleChange = (field: keyof CreateContestPayload, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));

    // Auto-generate slug from name
    if (field === 'name' && !formData.slug) {
      const slug = value
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .trim();
      setFormData((prev) => ({ ...prev, slug }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name || !formData.slug) {
      toast.error('Name and slug are required');
      return;
    }

    // Validate slug format
    const slugRegex = /^[a-z0-9-]+$/;
    if (!slugRegex.test(formData.slug)) {
      toast.error('Slug must contain only lowercase letters, numbers, and hyphens');
      return;
    }

    // Validate dates
    if (formData.startTime && formData.endTime) {
      const start = new Date(formData.startTime);
      const end = new Date(formData.endTime);
      if (end <= start) {
        toast.error('End time must be after start time');
        return;
      }
    }

    try {
      setLoading(true);
      const contest = await contestService.createContest(formData);
      toast.success('Contest created successfully');
      navigate(`/contest/${contest.id}/challenges`);
    } catch (error: any) {
      toast.error(error.message || 'Failed to create contest');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box display="flex" alignItems="center" gap={2} mb={3}>
        <Button
          startIcon={<ArrowBack />}
          onClick={() => navigate('/contests')}
          sx={{ color: theme === 'dark' ? 'rgb(156, 163, 175)' : 'rgb(107, 114, 128)' }}
        >
          Back to Contests
        </Button>
      </Box>

      <Card>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" fontWeight="bold" mb={3}>
            Create New Contest
          </Typography>

          <form onSubmit={handleSubmit}>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Contest Name"
                  required
                  value={formData.name}
                  onChange={(e) => handleChange('name', e.target.value)}
                  placeholder="e.g., Spring 2024 CTF"
                />
              </Grid>

              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Slug"
                  required
                  value={formData.slug}
                  onChange={(e) => handleChange('slug', e.target.value)}
                  placeholder="e.g., spring-2024-ctf"
                  helperText="URL-friendly identifier (lowercase, numbers, hyphens only)"
                />
              </Grid>

              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Description"
                  multiline
                  rows={4}
                  value={formData.description}
                  onChange={(e) => handleChange('description', e.target.value)}
                  placeholder="Describe your contest..."
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Semester"
                  value={formData.semesterName}
                  onChange={(e) => handleChange('semesterName', e.target.value)}
                  placeholder="e.g., Spring 2024"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>User Mode</InputLabel>
                  <Select
                    value={formData.userMode}
                    label="User Mode"
                    onChange={(e) => handleChange('userMode', e.target.value)}
                  >
                    <MenuItem value="users">Individual Users</MenuItem>
                    <MenuItem value="teams">Teams</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Start Time"
                  type="datetime-local"
                  value={formData.startTime}
                  onChange={(e) => handleChange('startTime', e.target.value)}
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="End Time"
                  type="datetime-local"
                  value={formData.endTime}
                  onChange={(e) => handleChange('endTime', e.target.value)}
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>

              <Grid item xs={12}>
                <Box display="flex" gap={2} justifyContent="flex-end">
                  <Button
                    variant="outlined"
                    onClick={() => navigate('/contests')}
                    disabled={loading}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    variant="contained"
                    startIcon={<Save />}
                    disabled={loading}
                    sx={{
                      bgcolor: theme === 'dark' ? '#3b82f6' : '#2563eb',
                      '&:hover': {
                        bgcolor: theme === 'dark' ? '#2563eb' : '#1d4ed8',
                      },
                    }}
                  >
                    {loading ? 'Creating...' : 'Create Contest'}
                  </Button>
                </Box>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
